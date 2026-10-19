using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Application.Dtos;

public record CustomerDto(int Id, string FullName, string Email, string? Phone);

public record CreateCustomerRequest
{
    [Required, StringLength(150)] public string FullName { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(256)] public string Email { get; init; } = string.Empty;
    [StringLength(30)] public string? Phone { get; init; }
}