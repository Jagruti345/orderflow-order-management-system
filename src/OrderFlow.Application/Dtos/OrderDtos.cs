using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Application.Dtos;

public record OrderItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public record OrderDto(
    int Id, int CustomerId, string Status, decimal TotalAmount,
    DateTime CreatedAtUtc, IReadOnlyList<OrderItemDto> Items);

public record OrderLineRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; init; }
    [Range(1, 1000)] public int Quantity { get; init; }
}

public record CreateOrderRequest
{
    public int? CustomerId { get; init; }   // admins must provide it; customers' id comes from their token
    [Required, MinLength(1)] public List<OrderLineRequest> Items { get; init; } = new();
}