using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Services;

public interface IOrderService
{
    Task<IReadOnlyList<OrderDto>> GetAllAsync(OrderStatus? status, CancellationToken ct = default);
    Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderDto> PayAsync(int id, CancellationToken ct = default);
    Task<OrderDto> ShipAsync(int id, CancellationToken ct = default);
    Task<OrderDto> CancelAsync(int id, CancellationToken ct = default);
}

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _db;

    public OrderService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(OrderStatus? status, CancellationToken ct = default)
    {
        var query = _db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var orders = await query.OrderByDescending(o => o.CreatedAtUtc).ToListAsync(ct);
        return orders.Select(ToDto).ToList();   // TotalAmount is computed in C#, so map after the query
    }

    public async Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException($"Order {id} was not found.");
        return ToDto(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
            throw new NotFoundException($"Customer {request.CustomerId} was not found.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))    // becomes SQL: WHERE Id IN (...)
            .ToDictionaryAsync(p => p.Id, ct);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Products not found: {string.Join(", ", missing)}.");

        var order = new Order(request.CustomerId);
        foreach (var line in request.Items)
            order.AddItem(products[line.ProductId], line.Quantity);   // domain enforces stock rules

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);   // order, items and stock changes saved in ONE transaction
        return ToDto(order);
    }

    public async Task<OrderDto> PayAsync(int id, CancellationToken ct = default)
    {
        var order = await LoadTrackedAsync(id, ct);
        order.MarkAsPaid();
        await _db.SaveChangesAsync(ct);
        return ToDto(order);
    }

    public async Task<OrderDto> ShipAsync(int id, CancellationToken ct = default)
    {
        var order = await LoadTrackedAsync(id, ct);
        order.Ship();
        await _db.SaveChangesAsync(ct);
        return ToDto(order);
    }

    public async Task<OrderDto> CancelAsync(int id, CancellationToken ct = default)
    {
        var order = await LoadTrackedAsync(id, ct);
        order.Cancel();   // throws if the order is shipped or already cancelled

        // Put the stock back (this is the "Phase 4" note we left in Order.Cancel)
        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(ct);
        foreach (var item in order.Items)
            products.First(p => p.Id == item.ProductId).IncreaseStock(item.Quantity);

        await _db.SaveChangesAsync(ct);
        return ToDto(order);
    }

    private async Task<Order> LoadTrackedAsync(int id, CancellationToken ct) =>
        await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException($"Order {id} was not found.");

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.CustomerId, o.Status.ToString(), o.TotalAmount, o.CreatedAtUtc,
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal)).ToList());
}