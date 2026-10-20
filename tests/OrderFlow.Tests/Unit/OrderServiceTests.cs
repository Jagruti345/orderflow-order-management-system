using Moq;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Application.Services;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Tests.Unit;

public class OrderServiceTests
{
    private static ICurrentUser FakeUser(bool isAdmin, int? customerId = null)
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.IsAdmin).Returns(isAdmin);
        user.Setup(u => u.CustomerId).Returns(customerId);
        return user.Object;
    }

    private static async Task<Customer> AddCustomerAsync(AppDbContext db)
    {
        var customer = new Customer("Test Customer", $"{Guid.NewGuid():N}@example.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private static async Task<Product> AddProductAsync(AppDbContext db, int stock = 10)
    {
        var product = new Product("Keyboard", "Mechanical", 50m, stock);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    private static async Task<Order> AddOrderAsync(AppDbContext db, Customer customer, Product product, int quantity)
    {
        var order = new Order(customer.Id);
        order.AddItem(product, quantity);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task Customer_CannotReadAnotherCustomersOrder()
    {
        await using var db = TestDb.Create();
        var owner = await AddCustomerAsync(db);
        var stranger = await AddCustomerAsync(db);
        var product = await AddProductAsync(db);
        var order = await AddOrderAsync(db, owner, product, 1);

        var service = new OrderService(db, FakeUser(isAdmin: false, customerId: stranger.Id));

        // 404, not 403: we don't reveal that the order exists
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(order.Id));
    }

    [Fact]
    public async Task Customer_ListSeesOnlyOwnOrders_AdminSeesAll()
    {
        await using var db = TestDb.Create();
        var alice = await AddCustomerAsync(db);
        var bob = await AddCustomerAsync(db);
        var product = await AddProductAsync(db);
        await AddOrderAsync(db, alice, product, 1);
        await AddOrderAsync(db, bob, product, 1);

        var asAlice = await new OrderService(db, FakeUser(false, alice.Id)).GetAllAsync(new OrderQuery());
        var asAdmin = await new OrderService(db, FakeUser(true)).GetAllAsync(new OrderQuery());

        Assert.Equal(1, asAlice.TotalCount);
        Assert.Equal(2, asAdmin.TotalCount);
    }

    [Fact]
    public async Task Create_AsCustomer_IgnoresCustomerIdInTheRequest()
    {
        await using var db = TestDb.Create();
        var me = await AddCustomerAsync(db);
        var victim = await AddCustomerAsync(db);
        var product = await AddProductAsync(db, stock: 10);
        var service = new OrderService(db, FakeUser(false, me.Id));

        var result = await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = victim.Id,   // an attacker tries to order on someone else's account
            Items = new() { new OrderLineRequest { ProductId = product.Id, Quantity = 2 } }
        });

        Assert.Equal(me.Id, result.CustomerId);      // the token's identity wins
        Assert.Equal(8, product.StockQuantity);
        Assert.Equal(100m, result.TotalAmount);
    }

    [Fact]
    public async Task Create_InsufficientStock_ThrowsAndSavesNoOrder()
    {
        await using var db = TestDb.Create();
        var me = await AddCustomerAsync(db);
        var product = await AddProductAsync(db, stock: 1);
        var service = new OrderService(db, FakeUser(false, me.Id));

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(new CreateOrderRequest
        {
            Items = new() { new OrderLineRequest { ProductId = product.Id, Quantity = 5 } }
        }));

        Assert.Empty(db.Orders);
        Assert.Equal(1, product.StockQuantity);
    }

    [Fact]
    public async Task Create_UnknownProduct_ThrowsNotFound()
    {
        await using var db = TestDb.Create();
        var me = await AddCustomerAsync(db);
        var service = new OrderService(db, FakeUser(false, me.Id));

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(new CreateOrderRequest
        {
            Items = new() { new OrderLineRequest { ProductId = 12345, Quantity = 1 } }
        }));
    }

    [Fact]
    public async Task Cancel_RestoresProductStock()
    {
        await using var db = TestDb.Create();
        var customer = await AddCustomerAsync(db);
        var product = await AddProductAsync(db, stock: 10);
        var order = await AddOrderAsync(db, customer, product, 3);
        Assert.Equal(7, product.StockQuantity);

        var result = await new OrderService(db, FakeUser(true)).CancelAsync(order.Id);

        Assert.Equal("Cancelled", result.Status);
        Assert.Equal(10, product.StockQuantity);
    }
}