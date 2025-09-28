using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatewayService.AccountCharge.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices");
        b.HasKey(x => x.Id);

        // Invoice root
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(64);
        b.HasIndex(x => x.InvoiceNumber).IsUnique();

        b.Property(x => x.CustomerId).HasMaxLength(64);
        b.HasIndex(x => x.CustomerId);

        b.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.ExpiresAt);

        // ExpectedAmount (Money VO)
        b.OwnsOne(x => x.ExpectedAmount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("ExpectedAmount")
                 .HasColumnType("decimal(38, 18)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("ExpectedCurrency")
                 .HasMaxLength(16)
                 .IsRequired();
        });

        // Read-model/projection properties (not persisted)
        b.Ignore(x => x.TotalPaid);
        b.Ignore(x => x.Payments);

        // -----------------------------
        // InvoiceAddresses (owned collection)
        // -----------------------------
        b.OwnsMany(x => x.Addresses, nav =>
        {
            nav.ToTable("InvoiceAddresses");

            nav.WithOwner().HasForeignKey(a => a.InvoiceId);

            nav.HasKey(a => a.Id);
            nav.Property(a => a.Id).ValueGeneratedNever(); // client-generated Guid

            nav.Property(a => a.InvoiceId).HasColumnName("InvoiceId").IsRequired();

            nav.Property(a => a.WalletId).HasColumnName("WalletId").IsRequired();
            nav.Property(a => a.Currency).HasColumnName("WalletCurrency").HasMaxLength(16).IsRequired();
            nav.Property(a => a.Address).HasColumnName("DepositAddress").HasMaxLength(256).IsRequired();
            nav.Property(a => a.Network).HasColumnName("DepositNetwork").HasMaxLength(32);
            nav.Property(a => a.Tag).HasColumnName("DepositTag").HasMaxLength(128);
            nav.Property(a => a.CreatedAt).HasColumnName("CreatedAt").IsRequired();

            nav.HasIndex(a => a.InvoiceId);
            nav.HasIndex(a => a.CreatedAt);

            nav.HasIndex(a => new { a.InvoiceId, a.Address, a.Network, a.Tag, a.WalletId }).IsUnique();
        });

        // ------------------------------------
        // InvoiceAppliedDeposits (owned collection)
        // ------------------------------------
        b.OwnsMany(x => x.AppliedDeposits, nav =>
        {
            nav.ToTable("InvoiceAppliedDeposits");

            nav.WithOwner().HasForeignKey(d => d.InvoiceId);

            nav.HasKey(d => d.Id);
            nav.Property(d => d.Id).ValueGeneratedNever();

            nav.Property(d => d.ObservedAt).IsRequired();
            nav.Property(d => d.WasConfirmed).IsRequired();
            nav.Property(d => d.Confirmations).IsRequired();
            nav.Property(d => d.RequiredConfirmations).IsRequired();

            // ✅ TransactionHash به‌صورت اسکالر (با کانورتر) روی ستون TxHash ذخیره می‌شود
            nav.Property(d => d.TxHash)
               .HasConversion(
                    toProvider => toProvider.Value,                // TransactionHash -> string
                    fromProvider => new TransactionHash(fromProvider) // string -> TransactionHash
                )
               .HasColumnName("TxHash")
               .HasMaxLength(128)
               .IsRequired();

            // 👇 ایندکس یکتا روی InvoiceId + TxHash (دوباره‌اعمال نشدن یک Tx برای یک اینوویس)
            nav.HasIndex(d => new { d.InvoiceId, d.TxHash }).IsUnique();

            // Infrastructure/Persistence/Configurations/InvoiceConfiguration.cs
            nav.OwnsOne(d => d.Address, addr =>
            {
                addr.Property(x => x.Address)
                    .HasColumnName("DepositAddress")
                    .HasMaxLength(128)
                    .IsRequired();

                addr.Property(x => x.Network)
                    .HasColumnName("DepositNetwork")
                    .HasMaxLength(32);                // <-- remove .IsRequired()

                addr.Property(x => x.Tag)
                    .HasColumnName("DepositTag")
                    .HasMaxLength(128);
            });

            // Money VO
            nav.OwnsOne(d => d.Amount, money =>
            {
                money.Property(m => m.Amount)
                     .HasColumnName("Amount")
                     .HasColumnType("decimal(38, 18)")
                     .IsRequired();

                money.Property(m => m.Currency)
                     .HasColumnName("Currency")
                     .HasMaxLength(16)
                     .IsRequired();
            });

            nav.HasIndex(d => d.InvoiceId);
            nav.HasIndex(d => d.ObservedAt);
        });

        // Backing-field access
        b.Navigation(x => x.Addresses).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.AppliedDeposits).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Root helpful indexes
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.Status);
    }
}
