using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Ignore(i => i.LineTotal);

        builder.HasOne<Product>()
               .WithMany()
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Restrict);   // can't delete a product that has been ordered
    }
}