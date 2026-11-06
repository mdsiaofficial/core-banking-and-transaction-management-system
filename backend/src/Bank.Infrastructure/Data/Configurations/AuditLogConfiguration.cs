using Bank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.OccurredAt });
        builder.HasIndex(a => new { a.ActorUserId, a.OccurredAt });
        builder.HasIndex(a => a.CorrelationId);
        builder.HasIndex(a => a.OccurredAt);

        builder.Property(a => a.Action)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(128);

        builder.Property(a => a.ActorRole)
            .HasMaxLength(64);

        builder.Property(a => a.ActorIp)
            .HasMaxLength(45);

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(64);

        builder.Property(a => a.Outcome)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.Before)
            .HasColumnType("jsonb");

        builder.Property(a => a.After)
            .HasColumnType("jsonb");

        builder.Property(a => a.Metadata)
            .HasColumnType("jsonb");

        builder.Property(a => a.PrevHash)
            .HasMaxLength(128);

        builder.Property(a => a.RowHash)
            .HasMaxLength(128);
    }
}
