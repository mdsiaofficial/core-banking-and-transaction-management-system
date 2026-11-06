using Bank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", t =>
        {
            t.HasCheckConstraint("ck_accounts_overdraft", "kind <> 'Customer' OR balance + overdraft_limit >= 0");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountNumber)
            .HasMaxLength(13)
            .IsRequired();

        builder.HasIndex(a => a.AccountNumber)
            .IsUnique();

        builder.HasIndex(a => a.CustomerId);

        builder.Property(a => a.Kind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.ProductType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.LedgerClass)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(a => a.Balance)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(a => a.HeldAmount)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(a => a.OverdraftLimit)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(a => a.MinBalance)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(a => a.DailyDebitLimit)
            .HasPrecision(19, 4);

        builder.Property(a => a.LastEntrySeq)
            .IsRequired();

        builder.Property(a => a.Version)
            .IsConcurrencyToken();

        builder.HasMany(a => a.Entries)
            .WithOne(e => e.Account)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
