using OrderFlow.Domain.Common;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public class Product : Entity
{
    private Product() { } // required by EF Core

    public Product(string name, string description, decimal price, int stockQuantity)
    {
        Name = ValidateName(name);
        Description = description ?? string.Empty;
        Price = ValidatePrice(price);
        StockQuantity = ValidateStock(stockQuantity);
    }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }

    public void UpdateDetails(string name, string description, decimal price)
    {
        Name = ValidateName(name);
        Description = description ?? string.Empty;
        Price = ValidatePrice(price);
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be greater than zero.");
        StockQuantity += quantity;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be greater than zero.");
        if (quantity > StockQuantity)
            throw new DomainException($"Insufficient stock for '{Name}'. Available: {StockQuantity}.");
        StockQuantity -= quantity;
    }

    private static string ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? throw new DomainException("Product name is required.")
            : name.Trim();

    private static decimal ValidatePrice(decimal price) =>
        price <= 0 ? throw new DomainException("Price must be greater than zero.") : price;

    private static int ValidateStock(int stock) =>
        stock < 0 ? throw new DomainException("Stock cannot be negative.") : stock;
}