using OrderFlow.Domain.Common;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public class OrderItem : Entity
{
    private OrderItem() { } // required by EF Core

    internal OrderItem(Product product, int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be greater than zero.");

        ProductId = product.Id;
        ProductName = product.Name;     // snapshot: later price/name changes don't alter old orders
        UnitPrice = product.Price;
        Quantity = quantity;
    }

    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    internal void IncreaseQuantity(int quantity) => Quantity += quantity;
}