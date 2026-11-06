using Bank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Infrastructure.Data.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.AccountId, e.AccountSeq })
            .IsUnique();

        builder.HasIndex(e => new { e.AccountId, e.PostedAt, e.Id });

        builder.HasIndex(e => e.TransactionId);

        builder.Property(e => e.Direction)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(e => e.BalanceAfter)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.AccountSeq)
            .IsRequired();
    }
}
