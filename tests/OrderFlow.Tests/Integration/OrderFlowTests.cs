using System.Net;
using System.Net.Http.Json;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;

namespace OrderFlow.Tests.Integration;

[Collection("Api")]
[Trait("Category", "Integration")]
public class OrderFlowTests
{
    private readonly ApiFactory _factory;

    public OrderFlowTests(ApiFactory factory) => _factory = factory;

    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    [Fact]
    public async Task Customer_PlacesOrder_StockGoesDown_CancelRestoresIt()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var customer = await _factory.CreateCustomerClientAsync();
        var product = await TestData.CreateProductAsync(admin, UniqueName("Stock"), stock: 10, price: 25m);

        var created = await TestData.PlaceOrderAsync(customer, product.Id, 3);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var order = (await created.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal(75m, order.TotalAmount);

        var afterOrder = await admin.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        Assert.Equal(7, afterOrder!.StockQuantity);

        var cancel = await customer.PostAsync($"/api/orders/{order.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var afterCancel = await admin.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        Assert.Equal(10, afterCancel!.StockQuantity);
    }

    [Fact]
    public async Task Customer_CannotSeeAnotherCustomersOrder()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var alice = await _factory.CreateCustomerClientAsync();
        var bob = await _factory.CreateCustomerClientAsync();
        var product = await TestData.CreateProductAsync(admin, UniqueName("Private"), stock: 5);

        var order = (await (await TestData.PlaceOrderAsync(alice, product.Id, 1))
            .Content.ReadFromJsonAsync<OrderDto>())!;

        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/orders/{order.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PostAsync($"/api/orders/{order.Id}/cancel", null)).StatusCode);

        var bobsList = await bob.GetFromJsonAsync<PagedResult<OrderDto>>("/api/orders");
        Assert.Equal(0, bobsList!.TotalCount);

        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync($"/api/orders/{order.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/orders/{order.Id}")).StatusCode);
    }

    [Fact]
    public async Task Order_MoreThanStock_Returns400()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var customer = await _factory.CreateCustomerClientAsync();
        var product = await TestData.CreateProductAsync(admin, UniqueName("Scarce"), stock: 2);

        var response = await TestData.PlaceOrderAsync(customer, product.Id, 5);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Order_FullLifecycle_PayThenAdminShips()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var customer = await _factory.CreateCustomerClientAsync();
        var product = await TestData.CreateProductAsync(admin, UniqueName("Life"), stock: 5);
        var order = (await (await TestData.PlaceOrderAsync(customer, product.Id, 1))
            .Content.ReadFromJsonAsync<OrderDto>())!;

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync($"/api/orders/{order.Id}/ship", null)).StatusCode); // not paid yet
        Assert.Equal(HttpStatusCode.OK, (await customer.PostAsync($"/api/orders/{order.Id}/pay", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsync($"/api/orders/{order.Id}/ship", null)).StatusCode);

        var shipped = await admin.PostAsync($"/api/orders/{order.Id}/ship", null);
        Assert.Equal(HttpStatusCode.OK, shipped.StatusCode);
        Assert.Equal("Shipped", (await shipped.Content.ReadFromJsonAsync<OrderDto>())!.Status);

        Assert.Equal(HttpStatusCode.BadRequest, (await customer.PostAsync($"/api/orders/{order.Id}/cancel", null)).StatusCode);
    }

    [Fact]
    public async Task LastItem_FiveBuyersAtOnce_ExactlyOneSucceeds_NoOverselling()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var product = await TestData.CreateProductAsync(admin, UniqueName("LastOne"), stock: 1);

        var buyers = new List<HttpClient>();
        for (var i = 0; i < 5; i++)
            buyers.Add(await _factory.CreateCustomerClientAsync());

        // Fire all five requests at the same time
        var responses = await Task.WhenAll(buyers.Select(b => TestData.PlaceOrderAsync(b, product.Id, 1)));

        var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(1, succeeded);

        // Losers get 400 (they saw stock = 0) or 409 (they raced and lost on the rowversion check)
        Assert.All(
            responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.True(r.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict,
                $"Unexpected status {r.StatusCode}"));

        var after = await admin.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        Assert.Equal(0, after!.StockQuantity);   // never negative, never oversold
    }
}