using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}