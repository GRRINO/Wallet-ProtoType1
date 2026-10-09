using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Project1_WalletPJ.Models;
using System.Text.RegularExpressions;
using static Project1_WalletPJ.Dtos.WalletDtos;

namespace Project1_WalletPJ.Features
{
    public class BL_Account
    {
        private const decimal MaxPerTransaction = 50_000m;
        private static readonly Regex MobileRegex = new(@"^\+?\d{8,14}$");
        private static readonly Regex PinRegex = new(@"^\d{4,6}$");

        private readonly DA_Account _da;
        private readonly PasswordHasher<TblWallet> _hasher = new();

        public BL_Account(DA_Account da)
        {
            _da = da;
        }

        public async Task<WalletResponse> RegisterAsync(RegisterRequest r)
        {
            if (string.IsNullOrWhiteSpace(r.FullName)) throw new ArgumentException("Full name is required.");
            if (!MobileRegex.IsMatch(r.MobileNo)) throw new ArgumentException("Invalid mobile number.");
            if (!PinRegex.IsMatch(r.Pin)) throw new ArgumentException("PIN must be 4-6 digits.");
            if (await _da.MobileExistAsync(r.MobileNo))
                throw new InvalidOperationException("Mobile number already has a wallet.");

            var wallet = new TblWallet { FullName = r.FullName.Trim(), MobileNo = r.MobileNo, Balance = 0, IsActive = true };
            wallet.PinHash = _hasher.HashPassword(wallet, r.Pin);
            await _da.CreateAsync(wallet);
            return ToResponse(wallet);
        }

        public async Task<WalletResponse> GetWalletAsync(string mobileNo) =>
        ToResponse(await GetWalletOrThrow(mobileNo));

        public async Task SetActiveAsync(string mobileNo, bool isActive)
        {
            var w = await GetWalletOrThrow(mobileNo);
            w.IsActive = isActive;
            await _da.SaveChangesAsync();
        }

        public async Task ChangePinAsync(string mobileNo, ChangePinRequest r)
        {
            if (!PinRegex.IsMatch(r.NewPin)) throw new ArgumentException("PIN must be 4-6 digits.");
            var w = await GetWalletOrThrow(mobileNo);
            VerifyPin(w, r.OldPin);
            w.PinHash = _hasher.HashPassword(w, r.NewPin);
            await _da.SaveChangesAsync();
        }


        public Task<TransactionResponse> DepositAsync(string mobileNo, decimal amount, string? note)
        {
            ValidateAmount(amount);
            return InTransactionAsync(async () =>
            {
                var w = await GetActiveWallet(mobileNo);
                w.Balance += amount;

                var t = NewTxn(TxnType.Deposit, null, w.WalletId, amount, note);
                _da.AddTransaction(t);
                _da.AddHistory(NewHistory(w, t, EntryType.Deposit, amount));
                await _da.SaveChangesAsync();
                return new TransactionResponse(t.TransactionNo, t.TransactionType, amount, w.Balance, t.TransactionDate);
            });
        }

        public Task<TransactionResponse> WithdrawAsync(string mobileNo, decimal amount, string pin, string? note)
        {
            ValidateAmount(amount);
            return InTransactionAsync(async () =>
            {
                var w = await GetActiveWallet(mobileNo);
                VerifyPin(w, pin);
                if (w.Balance < amount) throw new InvalidOperationException("Insufficient balance.");
                w.Balance -= amount;

                var t = NewTxn(TxnType.Withdraw, w.WalletId, null, amount, note);
                _da.AddTransaction(t);
                _da.AddHistory(NewHistory(w, t, EntryType.Withdraw, amount));
                await _da.SaveChangesAsync();
                return new TransactionResponse(t.TransactionNo, t.TransactionType, amount, w.Balance, t.TransactionDate);
            });
        }

        public Task<TransactionResponse> TransferAsync(string fromMobile, TransferRequest r)
        {
            ValidateAmount(r.Amount);
            if (fromMobile == r.ToMobileNo) throw new InvalidOperationException("Cannot transfer to the same wallet.");

            return InTransactionAsync(async () =>
            {
                var from = await GetActiveWallet(fromMobile);
                var to = await _da.GetByMobileAsync(r.ToMobileNo)
                         ?? throw new KeyNotFoundException("Destination wallet not found.");
                if (!to.IsActive) throw new InvalidOperationException("Destination wallet is inactive.");

                VerifyPin(from, r.Pin);
                if (from.Balance < r.Amount) throw new InvalidOperationException("Insufficient balance.");

                from.Balance -= r.Amount;
                to.Balance += r.Amount;

                var t = NewTxn(TxnType.Transfer, from.WalletId, to.WalletId, r.Amount, r.Note);
                _da.AddTransaction(t);
                _da.AddHistory(NewHistory(from, t, EntryType.TransferOut, r.Amount));
                _da.AddHistory(NewHistory(to, t, EntryType.TransferIn, r.Amount));
                await _da.SaveChangesAsync();
                return new TransactionResponse(t.TransactionNo, t.TransactionType, r.Amount, from.Balance, t.TransactionDate);
            });
        }

        public async Task<PagedResult<StatementItem>> GetStatementAsync(
    string mobileNo, DateTime? from, DateTime? to, int page, int pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var w = await GetWalletOrThrow(mobileNo);
            var (items, total) = await _da.GetHistoryAsync(w.WalletId, from, to, page, pageSize);

            var list = items.Select(h =>
            {
                var t = h.Transaction;
                string? other = h.EntryType switch
                {
                    EntryType.TransferOut => t.ToWallet?.MobileNo,
                    EntryType.TransferIn => t.FromWallet?.MobileNo,
                    _ => null
                };
                return new StatementItem(t.TransactionNo, h.EntryType, h.Amount, h.BalanceAfter, other, t.Note, h.CreatedDate);
            }).ToList();

            return new PagedResult<StatementItem>(list, total, page, pageSize);
        }



        private async Task<TblWallet> GetActiveWallet(string mobileNo)
        {
            var w = await GetWalletOrThrow(mobileNo);
            if (!w.IsActive) throw new InvalidOperationException("Wallet is inactive.");
            return w;
        }


        private async Task<T> InTransactionAsync<T>(Func<Task<T>> work)
        {
            await using var tx = await _da.BeginTransactionAsync();
            try
            {
                var result = await work();
                await tx.CommitAsync();
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                throw new InvalidOperationException("Wallet was modified by another request. Please try again.");
            }
        }


        private static void ValidateAmount(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.");
            if (amount > MaxPerTransaction) throw new ArgumentException($"Maximum per transaction is {MaxPerTransaction:N0}.");
            if (decimal.Round(amount, 2) != amount) throw new ArgumentException("Amount can have at most 2 decimal places.");
        }


        private void VerifyPin(TblWallet w, string pin)
        {
            var result = _hasher.VerifyHashedPassword(w, w.PinHash, pin);
            if (result == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Incorrect PIN.");
        }

        private async Task<TblWallet> GetWalletOrThrow(string mobileNo) =>
    await _da.GetByMobileAsync(mobileNo) ?? throw new KeyNotFoundException("Wallet not found.");

        private static TblTransaction NewTxn(string type, int? fromId, int? toId, decimal amount, string? note) => new()
        {
            TransactionNo = $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            TransactionType = type,
            FromWalletId = fromId,
            ToWalletId = toId,
            Amount = amount,
            Note = note,
            TransactionDate = DateTime.UtcNow
        };

        private static TblWalletHistory NewHistory(TblWallet w, TblTransaction t, string entryType, decimal amount) => new()
        {
            Wallet = w,
            Transaction = t,
            EntryType = entryType,
            Amount = amount,
            BalanceAfter = w.Balance,
            CreatedDate = t.TransactionDate
        };


        private static WalletResponse ToResponse(TblWallet w) =>
        new(w.WalletId, w.FullName, w.MobileNo, w.Balance, w.IsActive);
    }
}
