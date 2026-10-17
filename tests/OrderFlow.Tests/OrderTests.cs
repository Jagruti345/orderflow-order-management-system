using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;
using Xunit;

namespace OrderFlow.Tests;

public class OrderTests
{
    [Fact]
    public void AddItem_ReducesStock_AndCalculatesTotal()
    {
        var product = new Product("Keyboard", "Mechanical", 50m, 10);
        var order = new Order(customerId: 1);

        order.AddItem(product, 3);

        Assert.Equal(7, product.StockQuantity);
        Assert.Equal(150m, order.TotalAmount);
    }

    [Fact]
    public void AddItem_ThrowsWhenStockIsInsufficient()
    {
        var product = new Product("Mouse", "Wireless", 20m, 2);
        var order = new Order(1);

        Assert.Throws<DomainException>(() => order.AddItem(product, 5));
    }

    [Fact]
    public void Ship_ThrowsWhenOrderIsNotPaid()
    {
        var order = new Order(1);

        Assert.Throws<DomainException>(() => order.Ship());
    }

    [Fact]
    public void FullLifecycle_MovesThroughStatuses()
    {
        var product = new Product("Monitor", "27 inch", 200m, 5);
        var order = new Order(1);
        order.AddItem(product, 1);

        order.MarkAsPaid();
        order.Ship();

        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void Product_ThrowsWhenPriceIsNotPositive()
    {
        Assert.Throws<DomainException>(() => new Product("Pen", "", 0m, 5));
    }
}
