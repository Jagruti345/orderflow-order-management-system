using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common;
using OrderFlow.Application.Dtos;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _tokens;

    public AuthService(IApplicationDbContext db, IPasswordHasher hasher, IJwtTokenGenerator tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct) ||
            await _db.Customers.AnyAsync(c => c.Email == email, ct))
            throw new ConflictException("An account with this email already exists.");

        // Every registered user is a Customer with a linked Customer record. Role is never chosen by the caller.
        var customer = new Customer(request.FullName, email);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);   // saved first so we get customer.Id

        var user = new User(request.FullName, email, _hasher.Hash(request.Password), UserRole.Customer, customer.Id);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return ToResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Same message for "no such user" and "wrong password", so attackers can't discover which emails exist
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new AuthenticationFailedException("Invalid email or password.");

        return ToResponse(user);
    }

    private AuthResponse ToResponse(User user)
    {
        var token = _tokens.Generate(user);
        return new AuthResponse(token.Token, token.ExpiresAtUtc, user.Email, user.Role.ToString());
    }
}