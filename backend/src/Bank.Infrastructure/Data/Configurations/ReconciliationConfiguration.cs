using Bank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Infrastructure.Data.Configurations;

public class ReconciliationRunConfiguration : IEntityTypeConfiguration<ReconciliationRun>
{
    public void Configure(EntityTypeBuilder<ReconciliationRun> builder)
    {
        builder.ToTable("reconciliation_runs");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasMany(r => r.Findings)
            .WithOne(f => f.Run)
            .HasForeignKey(f => f.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReconciliationFindingConfiguration : IEntityTypeConfiguration<ReconciliationFinding>
{
    public void Configure(EntityTypeBuilder<ReconciliationFinding> builder)
    {
        builder.ToTable("reconciliation_findings");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.ExpectedBalance)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(f => f.ActualBalance)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(f => f.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(f => f.Severity)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(f => f.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(f => f.Account)
            .WithMany()
            .HasForeignKey(f => f.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
