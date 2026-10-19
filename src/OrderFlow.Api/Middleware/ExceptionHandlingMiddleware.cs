using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Common;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                DomainException => (StatusCodes.Status400BadRequest, "Business rule violated"),
                _ => (StatusCodes.Status500InternalServerError, "Server error")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                // never leak internal error details to the client
                Detail = status == 500 ? "An unexpected error occurred." : ex.Message
            });
        }
    }
}