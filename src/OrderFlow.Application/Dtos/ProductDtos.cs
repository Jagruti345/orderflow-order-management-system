using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Application.Dtos;

public record ProductDto(int Id, string Name, string Description, decimal Price, int StockQuantity);

/// <summary>Data needed to create a product.</summary>
public record CreateProductRequest
{
    /// <summary>Display name of the product.</summary>
    /// <example>Mechanical Keyboard</example>
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;

    /// <summary>Optional longer description.</summary>
    /// <example>Hot-swappable switches, USB-C</example>
    [StringLength(1000)] public string? Description { get; init; }

    /// <summary>Unit price. Must be greater than zero.</summary>
    /// <example>79.99</example>
    [Range(0.01, 1_000_000)] public decimal Price { get; init; }

    /// <summary>Units in stock.</summary>
    /// <example>25</example>
    [Range(0, 1_000_000)] public int StockQuantity { get; init; }
}

public record UpdateProductRequest
{
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;
    [StringLength(1000)] public string? Description { get; init; }
    [Range(0.01, 1_000_000)] public decimal Price { get; init; }
}