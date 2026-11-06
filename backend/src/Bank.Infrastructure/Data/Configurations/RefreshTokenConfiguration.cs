using Bank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(r => r.TokenHash)
            .IsUnique();

        builder.HasIndex(r => r.FamilyId);

        builder.HasIndex(r => r.UserId);

        builder.Property(r => r.CreatedByIp)
            .HasMaxLength(45);

        builder.Property(r => r.RevokedByIp)
            .HasMaxLength(45);

        builder.Property(r => r.UserAgent)
            .HasMaxLength(512);

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(r => r.ReplacedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
