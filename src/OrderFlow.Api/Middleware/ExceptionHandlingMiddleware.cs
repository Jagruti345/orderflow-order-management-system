using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var (status, title, detail) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", ex.Message),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
                AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Authentication failed", ex.Message),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
                DomainException => (StatusCodes.Status400BadRequest, "Business rule violated", ex.Message),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict",
                    "The data was changed by another request. Please retry."),
                _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            else if (ex is DbUpdateConcurrencyException)
                _logger.LogWarning("Concurrency conflict on {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = status;
            var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
            problem.Extensions["traceId"] = context.TraceIdentifier;   // the client can quote this when reporting a bug
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}