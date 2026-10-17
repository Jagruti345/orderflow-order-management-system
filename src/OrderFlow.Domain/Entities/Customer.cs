using OrderFlow.Domain.Common;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public class Customer : Entity
{
    private readonly List<Order> _orders = new();

    private Customer() { } // required by EF Core

    public Customer(string fullName, string email, string? phone = null)
    {
        FullName = string.IsNullOrWhiteSpace(fullName)
            ? throw new DomainException("Full name is required.")
            : fullName.Trim();

        Email = IsValidEmail(email)
            ? email.Trim().ToLowerInvariant()
            : throw new DomainException("A valid email is required.");

        Phone = phone?.Trim();
    }

    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@') && email.Contains('.');
}