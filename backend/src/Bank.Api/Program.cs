using Bank.Api.Endpoints;
using Bank.Api.Filters;
using Bank.Api.Middleware;
using Bank.Api.Security;
using Bank.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Logging & Configuration
builder.Services.AddLogging();

// Core Infrastructure & Persistence
builder.Services.AddInfrastructure(builder.Configuration);

// Security & Authorization
builder.Services.AddBankAuthorization();
builder.Services.AddBankRateLimiting();
builder.Services.AddScoped<ApplicationDbContextLocator>();
builder.Services.AddScoped<IdempotencyEndpointFilter>();

// RFC 9457 Problem Details Exception Handler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

// OpenAPI & Scalar
builder.Services.AddOpenApi();

// CORS for Angular Web Client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Pipeline Order per PLAN §8.4
app.UseExceptionHandler();

app.UseSecurityHeaders();

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue("ENABLE_OPENAPI", true))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Map Endpoint Slices
app.MapAuthEndpoints();
app.MapAccountEndpoints();
app.MapPaymentEndpoints();
app.MapAdminEndpoints();
app.MapCustomerEndpoints();
app.MapAuditEndpoints();

// Health Check Endpoints
app.MapGet("/", () => Results.Ok(new
{
    status = "healthy",
    service = "core-banking-api",
    version = "1.0.0",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/health/ready", () => Results.Ok(new
{
    status = "ready",
    timestamp = DateTimeOffset.UtcNow
}));

app.Run();

public partial class Program;
