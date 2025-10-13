using GatewayService.AccountCharge.Domain.PrepaidInvoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatewayService.AccountCharge.Infrastructure.Persistence.Configurations;

public sealed class PrepaidInvoiceConfiguration : IEntityTypeConfiguration<PrepaidInvoice>
{
    public void Configure(EntityTypeBuilder<PrepaidInvoice> b)
    {
        b.ToTable("PrepaidInvoices");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.CustomerId).HasMaxLength(64);
        b.Property(x => x.Currency).HasMaxLength(16).IsRequired();
        b.Property(x => x.Network).HasMaxLength(32);
        b.Property(x => x.TxHash).HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.TxHash).IsUnique();

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.ExpiresAt);

        b.Property(x => x.ObservedAmount).HasColumnType("decimal(38, 18)");
        b.Property(x => x.ObservedCurrency).HasMaxLength(16);
        b.Property(x => x.ObservedAddress).HasMaxLength(256);
        b.Property(x => x.ObservedTag).HasMaxLength(128);
        b.Property(x => x.ObservedWalletId);
        b.Property(x => x.ConfirmationsObserved);
        b.Property(x => x.RequiredConfirmationsObserved);
        b.Property(x => x.ConfirmedAt);
        b.Property(x => x.LastCheckedAt);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedAt);
    }
}
