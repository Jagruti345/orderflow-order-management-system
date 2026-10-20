using System.Net;
using System.Net.Http.Json;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;

namespace OrderFlow.Tests.Integration;

[Collection("Api")]
[Trait("Category", "Integration")]
public class AuthAndAccessTests
{
    private readonly ApiFactory _factory;

    public AuthAndAccessTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Orders_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Products_WithoutToken_Returns200()
    {
        var response = await _factory.CreateClient().GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AsCustomer_Returns403()
    {
        var customer = await _factory.CreateCustomerClientAsync();

        var response = await customer.PostAsJsonAsync("/api/products",
            new { name = "Hack", price = 1, stockQuantity = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);   // logged in, wrong role
    }

    [Fact]
    public async Task ListCustomers_AsCustomer_Returns403_AsAdmin_Returns200()
    {
        var customer = await _factory.CreateCustomerClientAsync();
        var admin = await _factory.CreateAdminClientAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/customers")).StatusCode);
    }

    [Fact]
    public async Task Ship_AsCustomer_Returns403()
    {
        var customer = await _factory.CreateCustomerClientAsync();

        var response = await customer.PostAsync("/api/orders/1/ship", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = ApiFactory.AdminEmail, password = "definitely-wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_SameEmailTwice_Returns409()
    {
        var client = _factory.CreateClient();
        var body = new { fullName = "Dup", email = $"{Guid.NewGuid():N}@example.com", password = "Passw0rd!" };

        var first = await client.PostAsJsonAsync("/api/auth/register", body);
        var second = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { fullName = "Short", email = $"{Guid.NewGuid():N}@example.com", password = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);   // from [StringLength(MinimumLength = 8)]
    }

    [Fact]
    public async Task Products_PageSizeAboveCap_Returns400()
    {
        var response = await _factory.CreateClient().GetAsync("/api/products?pageSize=1000");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Products_Pagination_ReturnsCorrectPageAndTotals()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var prefix = "Paging" + Guid.NewGuid().ToString("N")[..8];
        for (var i = 1; i <= 3; i++)
            await TestData.CreateProductAsync(admin, $"{prefix}-{i}", stock: 1);

        var page = await admin.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?search={prefix}&page=1&pageSize=2");

        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
    }
}