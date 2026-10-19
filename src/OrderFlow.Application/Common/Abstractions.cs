using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public record TokenResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    TokenResult Generate(User user);
}

// Who is making the current request? Implemented in the Api layer.
public interface ICurrentUser
{
    int? UserId { get; }
    int? CustomerId { get; }
    bool IsAdmin { get; }
}