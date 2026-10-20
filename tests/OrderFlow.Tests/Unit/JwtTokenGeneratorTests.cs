using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Auth;

namespace OrderFlow.Tests.Unit;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator CreateGenerator() =>
        new(Options.Create(new JwtSettings
        {
            Key = new string('k', 64),
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiryMinutes = 5
        }));

    [Fact]
    public void Generate_PutsRoleAndCustomerIdInTheToken()
    {
        var user = new User("Jane", "jane@example.com", "some-hash", UserRole.Customer, customerId: 7);

        var result = CreateGenerator().Generate(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal("Customer", jwt.Claims.First(c => c.Type == "role").Value);
        Assert.Equal("7", jwt.Claims.First(c => c.Type == "customer_id").Value);
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void Generate_NeverIncludesThePasswordHash()
    {
        // A JWT is signed, not encrypted: anyone can read it
        var user = new User("Jane", "jane@example.com", "SECRET-HASH", UserRole.Customer, 7);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(CreateGenerator().Generate(user).Token);

        Assert.DoesNotContain(jwt.Claims, c => c.Value.Contains("SECRET-HASH"));
    }
}