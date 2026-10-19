using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Application.Dtos;

public record RegisterRequest
{
    [Required, StringLength(150)] public string FullName { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(256)] public string Email { get; init; } = string.Empty;
    [Required, StringLength(72, MinimumLength = 8)] public string Password { get; init; } = string.Empty;
}

public record LoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
}

public record AuthResponse(string Token, DateTime ExpiresAtUtc, string Email, string Role);