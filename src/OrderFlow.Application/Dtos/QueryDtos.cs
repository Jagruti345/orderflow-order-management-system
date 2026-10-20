using System.ComponentModel.DataAnnotations;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Dtos;

public record PageQuery
{
    [Range(1, 100_000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;   // hard cap: clients can't ask for 1 million rows
}

public record ProductQuery : PageQuery
{
    [StringLength(100)] public string? Search { get; init; }
    public bool? InStock { get; init; }
}

public record OrderQuery : PageQuery
{
    public OrderStatus? Status { get; init; }
}