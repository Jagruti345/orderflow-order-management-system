using OrderFlow.Domain.Common;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public class Order : Entity
{
    private readonly List<OrderItem> _items = new();

    private Order() { } // required by EF Core

    public Order(int customerId)
    {
        if (customerId <= 0) throw new DomainException("A valid customer is required.");
        CustomerId = customerId;
        Status = OrderStatus.Pending;
    }

    public int CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal TotalAmount => _items.Sum(i => i.LineTotal);

    public void AddItem(Product product, int quantity)
    {
        EnsureStatus(OrderStatus.Pending, "Items can only be added to a pending order.");

        product.DecreaseStock(quantity); // business rule lives in Product

        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is null)
            _items.Add(new OrderItem(product, quantity));
        else
            existing.IncreaseQuantity(quantity);
    }

    public void MarkAsPaid()
    {
        EnsureStatus(OrderStatus.Pending, "Only a pending order can be paid.");
        if (_items.Count == 0) throw new DomainException("Cannot pay for an empty order.");
        Status = OrderStatus.Paid;
    }

    public void Ship()
    {
        EnsureStatus(OrderStatus.Paid, "Only a paid order can be shipped.");
        Status = OrderStatus.Shipped;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Cancelled)
            throw new DomainException($"A {Status} order cannot be cancelled.");
        Status = OrderStatus.Cancelled;
        // Phase 4: the application service will restore product stock when cancelling.
    }

    private void EnsureStatus(OrderStatus required, string message)
    {
        if (Status != required) throw new DomainException(message);
    }
}
