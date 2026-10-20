using Microsoft.EntityFrameworkCore;
using Moq;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Application.Services;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Tests.Unit;

public class AuthServiceTests
{
    private static (AuthService Service, Mock<IJwtTokenGenerator> Tokens) Build(AppDbContext db)
    {
        // Fake hasher: "hashing" just adds a prefix, so the tests run instantly
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash(It.IsAny<string>()))
              .Returns<string>(password => $"hashed:{password}");
        hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
              .Returns<string, string>((password, hash) => hash == $"hashed:{password}");

        var tokens = new Mock<IJwtTokenGenerator>();
        tokens.Setup(t => t.Generate(It.IsAny<User>()))
              .Returns(new TokenResult("fake-token", DateTime.UtcNow.AddHours(1)));

        return (new AuthService(db, hasher.Object, tokens.Object), tokens);
    }

    private static RegisterRequest NewUser(string email = "jane@example.com") =>
        new() { FullName = "Jane", Email = email, Password = "Passw0rd!" };

    [Fact]
    public async Task Register_CreatesCustomerRoleUser_AndStoresOnlyTheHash()
    {
        await using var db = TestDb.Create();
        var (service, _) = Build(db);

        var response = await service.RegisterAsync(NewUser());

        Assert.Equal("Customer", response.Role);                 // callers can never choose a role
        Assert.Equal("fake-token", response.Token);
        var user = await db.Users.SingleAsync();
        Assert.NotEqual("Passw0rd!", user.PasswordHash);          // the plain password is never stored
        Assert.Equal(1, await db.Customers.CountAsync());         // a linked Customer record exists
        Assert.NotNull(user.CustomerId);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        await using var db = TestDb.Create();
        var (service, _) = Build(db);
        await service.RegisterAsync(NewUser());

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(NewUser("JANE@example.com")));
    }

    [Fact]
    public async Task Login_CorrectPassword_ReturnsTokenOnce()
    {
        await using var db = TestDb.Create();
        var (service, tokens) = Build(db);
        await service.RegisterAsync(NewUser());
        tokens.Invocations.Clear();   // forget the token generated during registration

        var response = await service.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "Passw0rd!" });

        Assert.Equal("fake-token", response.Token);
        tokens.Verify(t => t.Generate(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsAuthenticationFailed()
    {
        await using var db = TestDb.Create();
        var (service, _) = Build(db);
        await service.RegisterAsync(NewUser());

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            service.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "nope" }));
    }

    [Fact]
    public async Task Login_UnknownEmail_GivesSameErrorAsWrongPassword()
    {
        // Security property: attackers must not be able to learn which emails are registered
        await using var db = TestDb.Create();
        var (service, _) = Build(db);
        await service.RegisterAsync(NewUser());

        var unknown = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            service.LoginAsync(new LoginRequest { Email = "ghost@example.com", Password = "x" }));
        var wrong = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            service.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "x" }));

        Assert.Equal(wrong.Message, unknown.Message);
    }
}