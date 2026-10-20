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

    [Fact]
public void Cancel_ThrowsWhenOrderIsShipped()
{
    // Arrange
    var product = new Product("Headset", "Wired", 80m, 5);
    var order = new Order(1);
    order.AddItem(product, 1);
    order.MarkAsPaid();
    order.Ship();

    // Act + Assert
    Assert.Throws<DomainException>(() => order.Cancel());
}

[Fact]
public void AddItem_SameProductTwice_MergesIntoOneItem()
{
    // Arrange
    var product = new Product("Cable", "USB-C", 10m, 10);
    var order = new Order(1);

    // Act
    order.AddItem(product, 2);
    order.AddItem(product, 3);

    // Assert
    Assert.Single(order.Items);                       // only one OrderItem
    Assert.Equal(5, order.Items.First().Quantity);    // 2 + 3
    Assert.Equal(50m, order.TotalAmount);             // 5 x 10
    Assert.Equal(5, product.StockQuantity);           // 10 - 5
}
}


