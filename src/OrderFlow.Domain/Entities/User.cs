using OrderFlow.Domain.Common;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public class User : Entity
{
    private User() { } // required by EF Core

    public User(string fullName, string email, string passwordHash, UserRole role, int? customerId = null)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Full name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        CustomerId = customerId;
    }

    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public int? CustomerId { get; private set; }   // links a Customer-role user to their Customer record
}