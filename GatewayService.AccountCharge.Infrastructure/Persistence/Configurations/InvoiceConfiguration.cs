using GatewayService.AccountCharge.Domain.Invoices;
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
        // InvoiceAddresses (owned collection, اسکالر)
        // -----------------------------
        b.OwnsMany(x => x.Addresses, nav =>
        {
            nav.ToTable("InvoiceAddresses");

            // Explicit FK (no shadow)
            nav.WithOwner().HasForeignKey(a => a.InvoiceId);

            // Keys
            nav.HasKey(a => a.Id);
            nav.Property(a => a.Id).ValueGeneratedNever(); // client-generated Guid

            // (optional) map FK column name explicitly
            nav.Property(a => a.InvoiceId).HasColumnName("InvoiceId").IsRequired();

            // Fields
            nav.Property(a => a.WalletId).HasColumnName("WalletId").IsRequired();
            nav.Property(a => a.Currency).HasColumnName("WalletCurrency").HasMaxLength(16).IsRequired();
            nav.Property(a => a.Address).HasColumnName("DepositAddress").HasMaxLength(256).IsRequired();
            nav.Property(a => a.Network).HasColumnName("DepositNetwork").HasMaxLength(32);
            nav.Property(a => a.Tag).HasColumnName("DepositTag").HasMaxLength(128);
            nav.Property(a => a.CreatedAt).HasColumnName("CreatedAt").IsRequired();

            // Indexes
            nav.HasIndex(a => a.InvoiceId);
            nav.HasIndex(a => a.CreatedAt);

            // Unique constraint to avoid duplicates per invoice/wallet
            nav.HasIndex(a => new { a.InvoiceId, a.Address, a.Network, a.Tag, a.WalletId }).IsUnique();
        });


        // ------------------------------------
        // InvoiceAppliedDeposits (owned collection) — طبق مدل VO سابق شما
        // ------------------------------------
        b.OwnsMany(x => x.AppliedDeposits, nav =>
        {
            nav.ToTable("InvoiceAppliedDeposits");

            nav.WithOwner().HasForeignKey(d => d.InvoiceId);

            nav.HasKey(d => d.Id);
            nav.Property(d => d.Id).ValueGeneratedOnAdd();

            nav.Property(d => d.ObservedAt).IsRequired();
            nav.Property(d => d.WasConfirmed).IsRequired();
            nav.Property(d => d.Confirmations).IsRequired();
            nav.Property(d => d.RequiredConfirmations).IsRequired();

            // TransactionHash VO
            nav.OwnsOne(d => d.TxHash, h =>
            {
                h.Property(v => v.Value)
                 .HasColumnName("TxHash")
                 .HasMaxLength(128)
                 .IsRequired();

                // توجه: اگر EF روی OwnsOne اجازه‌ی ایندکس مستقیم نداد،
                // ایندکس را یک سطح بالاتر روی جدول بزنید:
                // nav.HasIndex("TxHash").IsUnique();
            });

            // ChainAddress VO
            nav.OwnsOne(d => d.Address, addr =>
            {
                addr.Property(x => x.Address)
                    .HasColumnName("DepositAddress")
                    .HasMaxLength(128)
                    .IsRequired();

                addr.Property(x => x.Network)
                    .HasColumnName("DepositNetwork")
                    .HasMaxLength(32)
                    .IsRequired();

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
