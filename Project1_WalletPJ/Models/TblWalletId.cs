using System;
using System.Collections.Generic;

namespace Project1_WalletPJ.Models;

public partial class TblWalletId
{
    public int WalletHistoryId { get; set; }

    public string MobileId { get; set; } = null!;

    public string TransactionType { get; set; } = null!;

    public decimal Amount { get; set; }
}
