using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.HasIndex(c => c.Email).IsUnique();   // no two customers share an email

        builder.HasMany(c => c.Orders)
               .WithOne()
               .HasForeignKey(o => o.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(c => c.Orders).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}