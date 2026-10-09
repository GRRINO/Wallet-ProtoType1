namespace Project1_WalletPJ.Dtos
{
    public class WalletDtos
    {
        public record RegisterRequest(string FullName, string MobileNo, string Pin);
        public record AmountRequest(decimal Amount, string? Note);
        public record WithdrawRequest(decimal Amount, string Pin, string? Note);
        public record TransferRequest(string ToMobileNo, decimal Amount, string Pin, string? Note);
        public record ChangePinRequest(string OldPin, string NewPin);

        public record WalletResponse(int WalletId, string FullName, string MobileNo, decimal Balance, bool IsActive);
        public record TransactionResponse(string TransactionNo, string Type, decimal Amount, decimal BalanceAfter, DateTime Date);
        public record StatementItem(string TransactionNo, string EntryType, decimal Amount,
            decimal BalanceAfter, string? Counterparty, string? Note, DateTime Date);
        public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);
    }
}
