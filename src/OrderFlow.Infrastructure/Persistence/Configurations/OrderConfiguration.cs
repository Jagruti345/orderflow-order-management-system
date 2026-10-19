using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20); // stored as text, e.g. "Paid"
        builder.Ignore(o => o.TotalAmount);                                       // computed, not a column

        builder.HasMany(o => o.Items)
               .WithOne()
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        // Use the private _items list, so the domain rules still apply
        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}