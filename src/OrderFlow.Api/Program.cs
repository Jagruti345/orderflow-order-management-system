using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderFlow.Api.Middleware;
using OrderFlow.Api.Services;
using OrderFlow.Application;
using OrderFlow.Application.Common;
using OrderFlow.Infrastructure;
using OrderFlow.Infrastructure.Auth;
using OrderFlow.Infrastructure.Persistence;
using Serilog;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging (Serilog reads its settings from appsettings.json) ----------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// ---------- Fail fast if the signing key is missing ----------
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters. Set it with dotnet user-secrets.");

// ---------- Services ----------
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "sub"
        };
    });
builder.Services.AddAuthorization();

// ---------- Rate limiting ----------
string ClientKey(HttpContext c) => c.Connection.RemoteIpAddress?.ToString() ?? "unknown";

var authLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitPerMinute", 10);
var globalLimit = builder.Configuration.GetValue("RateLimiting:GlobalPermitPerMinute", 200);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Strict limit for login/register: blocks password guessing
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // Generous limit for everything else: stops one client from flooding the API
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = globalLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = "Rate limit exceeded. Please wait and try again."
        }, ct);
    };
});

// ---------- Output caching ----------
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Catalog", policy => policy
        .Expire(TimeSpan.FromSeconds(60))
        .SetVaryByQuery("page", "pageSize", "search", "inStock")
        .Tag("products"));
});

// ---------- Health checks ----------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "ready" });

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the token. Swagger adds the 'Bearer ' prefix."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Apply pending migrations at startup. Off by default; docker-compose turns it on.
// (Large production systems usually run migrations as a separate deployment step instead.)
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await DbSeeder.SeedAdminAsync(app.Services, app.Configuration);

// ---------- Middleware pipeline (order matters) ----------
app.UseSerilogRequestLogging();                     // one log line per request: method, path, status, time
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseHttpsRedirection();
app.UseRateLimiter();        // reject abusive traffic before doing any real work
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();        // after authorization, so protected endpoints are never served from the cache

app.MapControllers();

// Liveness: is the process running? (runs no checks)
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
   .DisableRateLimiting();
// Readiness: can it serve traffic? (runs the database check)
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
   .DisableRateLimiting();

app.Run();
public partial class Program { }