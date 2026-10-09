using Project1_WalletPJ.Data;
using Project1_WalletPJ.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Project1_WalletPJ.Features
{
    public class DA_Account
    {
        private readonly AppDbContext _context;
        
        public DA_Account(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TblWallet?> GetByIdAsync(int id) => await _context.TblWallets.FirstOrDefaultAsync(w => w.WalletId == id);
        
        public async Task<TblWallet?> GetByMobileAsync(string mobileNo) => await _context.TblWallets.FirstOrDefaultAsync(w => w.MobileNo == mobileNo);
        
        public async Task<bool> MobileExistAsync(string mobileNo) => await _context.TblWallets.AnyAsync(w => w.MobileNo == mobileNo);

        public async Task<TblWallet> CreateAsync(TblWallet wallet)
        {
            _context.TblWallets.Add(wallet);
            await _context.SaveChangesAsync();
            return wallet;
        }

        public void AddHistory(TblWalletHistory history)
        {
            _context.TblWalletHistories.Add(history);
        }

        public void AddTransaction(TblTransaction transaction)
        {
            _context.TblTransactions.Add(transaction);
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<IDbContextTransaction> BeginTransactionAsync() =>
            _context.Database.BeginTransactionAsync();


        public async Task<(List<TblWalletHistory> Items, int Total)> GetHistoryAsync(
            int walletId, DateTime? from, DateTime? to, int page, int pageSize)
        {
            var q = _context.TblWalletHistories.AsNoTracking()
                .Include(h => h.Transaction).ThenInclude(t => t.FromWallet)
                .Include(h => h.Transaction).ThenInclude(t => t.ToWallet)
                .Where(h => h.WalletId == walletId);

            if (from.HasValue) q = q.Where(h => h.CreatedDate >= from.Value);
            if (to.HasValue) q = q.Where(h => h.CreatedDate < to.Value.Date.AddDays(1));

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(h => h.CreatedDate).ThenByDescending(h => h.WalletHistoryId)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }
    }
}
