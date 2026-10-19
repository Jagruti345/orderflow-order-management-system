using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Application.Dtos;

public record ProductDto(int Id, string Name, string Description, decimal Price, int StockQuantity);

public record CreateProductRequest
{
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;
    [StringLength(1000)] public string? Description { get; init; }
    [Range(0.01, 1_000_000)] public decimal Price { get; init; }
    [Range(0, 1_000_000)] public int StockQuantity { get; init; }
}

public record UpdateProductRequest
{
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;
    [StringLength(1000)] public string? Description { get; init; }
    [Range(0.01, 1_000_000)] public decimal Price { get; init; }
}