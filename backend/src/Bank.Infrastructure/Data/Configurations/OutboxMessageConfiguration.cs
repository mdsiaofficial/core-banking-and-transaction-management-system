using Bank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);

        builder.HasIndex(o => o.OccurredAt);

        builder.Property(o => o.Type)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(o => o.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
