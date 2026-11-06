using Bank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerNo)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(c => c.CustomerNo)
            .IsUnique();

        builder.HasIndex(c => c.UserId)
            .IsUnique();

        builder.Property(c => c.FullName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.Phone)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.AddressJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.KycStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasMany(c => c.Accounts)
            .WithOne(a => a.Customer)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
