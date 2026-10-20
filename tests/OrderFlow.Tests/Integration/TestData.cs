using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OrderFlow.Application.Dtos;

namespace OrderFlow.Tests.Integration;

public static class TestData
{
    private static HttpClient WithToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<HttpClient> CreateAdminClientAsync(this ApiFactory factory)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return WithToken(client, auth!.Token);
    }

    public static async Task<HttpClient> CreateCustomerClientAsync(this ApiFactory factory)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test Customer",
            email = $"{Guid.NewGuid():N}@example.com",
            password = "Passw0rd!"
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return WithToken(client, auth!.Token);
    }

    public static async Task<ProductDto> CreateProductAsync(HttpClient admin, string name, int stock, decimal price = 10m)
    {
        var response = await admin.PostAsJsonAsync("/api/products",
            new { name, description = "test", price, stockQuantity = stock });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    public static Task<HttpResponseMessage> PlaceOrderAsync(HttpClient customer, int productId, int quantity) =>
        customer.PostAsJsonAsync("/api/orders",
            new { items = new[] { new { productId, quantity } } });
}