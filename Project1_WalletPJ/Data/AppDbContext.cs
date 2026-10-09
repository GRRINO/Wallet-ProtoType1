using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Project1_WalletPJ.Models;

namespace Project1_WalletPJ.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblTransaction> TblTransactions { get; set; }

    public virtual DbSet<TblWallet> TblWallets { get; set; }

    public virtual DbSet<TblWalletHistory> TblWalletHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Tbl_Tran__55433A6B89687F77");

            entity.ToTable("Tbl_Transaction");

            entity.HasIndex(e => new { e.FromWalletId, e.TransactionDate }, "IX_Txn_From").IsDescending(false, true);

            entity.HasIndex(e => new { e.ToWalletId, e.TransactionDate }, "IX_Txn_To").IsDescending(false, true);

            entity.HasIndex(e => e.TransactionNo, "UQ__Tbl_Tran__554342D9436C968A").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Note).HasMaxLength(200);
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TransactionNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.FromWallet).WithMany(p => p.TblTransactionFromWallets)
                .HasForeignKey(d => d.FromWalletId)
                .HasConstraintName("FK_Txn_From");

            entity.HasOne(d => d.ToWallet).WithMany(p => p.TblTransactionToWallets)
                .HasForeignKey(d => d.ToWalletId)
                .HasConstraintName("FK_Txn_To");
        });

        modelBuilder.Entity<TblWallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__Tbl_Wall__84D4F90EA47BE449");

            entity.ToTable("Tbl_Wallet");

            entity.HasIndex(e => e.MobileNo, "UQ__Tbl_Wall__D6D73A86EBD6C065").IsUnique();

            entity.Property(e => e.Balance).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())", "DF_Wallet_Created");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Wallet_IsActive");
            entity.Property(e => e.MobileNo)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.PinHash)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasDefaultValue("", "DF_Wallet_PinHash");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblWalletHistory>(entity =>
        {
            entity.HasKey(e => e.WalletHistoryId);

            entity.ToTable("Tbl_WalletHistory");

            entity.HasIndex(e => new { e.WalletId, e.CreatedDate }, "IX_Hist_Wallet").IsDescending(false, true);

            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.BalanceAfter).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())", "DF_Hist_Created");
            entity.Property(e => e.EntryType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Transaction).WithMany(p => p.TblWalletHistories)
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Hist_Txn");

            entity.HasOne(d => d.Wallet).WithMany(p => p.TblWalletHistories)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Hist_Wallet");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
