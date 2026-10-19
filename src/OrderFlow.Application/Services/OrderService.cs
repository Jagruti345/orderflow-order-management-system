using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;

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
    private readonly ICurrentUser _currentUser;

    public OrderService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(OrderStatus? status, CancellationToken ct = default)
    {
        var query = _db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (!_currentUser.IsAdmin)
        {
            var customerId = _currentUser.CustomerId
                ?? throw new ForbiddenException("Your account is not linked to a customer.");
            query = query.Where(o => o.CustomerId == customerId);   // customers only see their own orders
        }

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var orders = await query.OrderByDescending(o => o.CreatedAtUtc).ToListAsync(ct);
        return orders.Select(ToDto).ToList();
    }

    public async Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException($"Order {id} was not found.");

        EnsureCanAccess(order);
        return ToDto(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        int customerId;
        if (_currentUser.IsAdmin)
            customerId = request.CustomerId
                ?? throw new DomainException("CustomerId is required when an admin creates an order.");
        else
            customerId = _currentUser.CustomerId
                ?? throw new ForbiddenException("Your account is not linked to a customer.");

        if (!await _db.Customers.AnyAsync(c => c.Id == customerId, ct))
            throw new NotFoundException($"Customer {customerId} was not found.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Products not found: {string.Join(", ", missing)}.");

        var order = new Order(customerId);
        foreach (var line in request.Items)
            order.AddItem(products[line.ProductId], line.Quantity);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
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
        order.Cancel();

        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(ct);
        foreach (var item in order.Items)
            products.First(p => p.Id == item.ProductId).IncreaseStock(item.Quantity);

        await _db.SaveChangesAsync(ct);
        return ToDto(order);
    }

    private async Task<Order> LoadTrackedAsync(int id, CancellationToken ct)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException($"Order {id} was not found.");
        EnsureCanAccess(order);
        return order;
    }

    // Admins can access everything; customers only their own orders.
    // We return 404 (not 403) so a customer can't discover that someone else's order id exists.
    private void EnsureCanAccess(Order order)
    {
        if (_currentUser.IsAdmin) return;
        if (_currentUser.CustomerId == order.CustomerId) return;
        throw new NotFoundException($"Order {order.Id} was not found.");
    }

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.CustomerId, o.Status.ToString(), o.TotalAmount, o.CreatedAtUtc,
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal)).ToList());
}