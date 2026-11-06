using Bank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Infrastructure.Data.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(i => i.Id);

        builder.HasIndex(i => new { i.UserId, i.Key })
            .IsUnique();

        builder.HasIndex(i => i.ExpiresAt);

        builder.Property(i => i.Key)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(i => i.Endpoint)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(i => i.RequestHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(i => i.ResponseBodyJson)
            .HasColumnType("jsonb");
    }
}
