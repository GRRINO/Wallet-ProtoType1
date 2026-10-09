using System;
using System.Collections.Generic;

namespace Project1_WalletPJ.Models;

public static class TxnType
{
    public const string Deposit = "Deposit";
    public const string Withdraw = "Withdraw";
    public const string Transfer = "Transfer";
}

public partial class TblTransaction
{
    public int TransactionId { get; set; }

    public string TransactionNo { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    public string TransactionType { get; set; } = null!;

    public int? FromWalletId { get; set; }

    public int? ToWalletId { get; set; }

    public string? Note { get; set; }

    public virtual TblWallet? FromWallet { get; set; }

    public virtual ICollection<TblWalletHistory> TblWalletHistories { get; set; } = new List<TblWalletHistory>();

    public virtual TblWallet? ToWallet { get; set; }
}
