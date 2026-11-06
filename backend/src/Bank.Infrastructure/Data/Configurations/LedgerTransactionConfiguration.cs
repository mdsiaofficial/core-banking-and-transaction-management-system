using Bank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Infrastructure.Data.Configurations;

public class LedgerTransactionConfiguration : IEntityTypeConfiguration<LedgerTransaction>
{
    public void Configure(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Reference)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(t => t.Reference)
            .IsUnique();

        builder.HasIndex(t => t.ReversesTransactionId)
            .IsUnique();

        builder.HasIndex(t => t.PostedAt);

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(t => t.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Channel)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.InitiatedByUserId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(t => t.MetadataJson)
            .HasColumnType("jsonb");

        builder.HasMany(t => t.Entries)
            .WithOne(e => e.Transaction)
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ReversesTransaction)
            .WithMany()
            .HasForeignKey(t => t.ReversesTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
