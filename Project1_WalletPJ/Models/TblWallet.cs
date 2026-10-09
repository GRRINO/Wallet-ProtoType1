using System;
using System.Collections.Generic;

namespace Project1_WalletPJ.Models;

public partial class TblWallet
{
    public int WalletId { get; set; }

    public string FullName { get; set; } = null!;

    public string MobileNo { get; set; } = null!;

    public decimal Balance { get; set; }

    public string PinHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<TblTransaction> TblTransactionFromWallets { get; set; } = new List<TblTransaction>();

    public virtual ICollection<TblTransaction> TblTransactionToWallets { get; set; } = new List<TblTransaction>();

    public virtual ICollection<TblWalletHistory> TblWalletHistories { get; set; } = new List<TblWalletHistory>();
}
