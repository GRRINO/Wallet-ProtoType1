using System;
using System.Collections.Generic;

namespace Project1_WalletPJ.Models;

public static class EntryType
{
    public const string Deposit = "Deposit";
    public const string Withdraw = "Withdraw";
    public const string TransferIn = "TransferIn";
    public const string TransferOut = "TransferOut";
}
public partial class TblWalletHistory
{
    public int WalletHistoryId { get; set; }

    public int WalletId { get; set; }

    public int TransactionId { get; set; }

    public string EntryType { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual TblTransaction Transaction { get; set; } = null!;

    public virtual TblWallet Wallet { get; set; } = null!;
}
