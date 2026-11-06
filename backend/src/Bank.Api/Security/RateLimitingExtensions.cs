using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Bank.Api.Security;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "AuthPolicy";
    public const string TransactionPolicy = "TransactionPolicy";

    public static IServiceCollection AddBankRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.io/429",
                    title = "Too Many Requests",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Request rate limit exceeded. Please retry after some time.",
                    errorCode = "RATE_LIMITED"
                }, cancellationToken: token);
            };

            // Per-IP rate limiting on authentication routes (prevent brute force)
            options.AddPolicy(AuthPolicy, httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            // Per-user rate limiting on financial transaction endpoints
            options.AddPolicy(TransactionPolicy, httpContext =>
            {
                var userId = httpContext.User.GetUserId()?.ToString()
                             ?? httpContext.Connection.RemoteIpAddress?.ToString()
                             ?? "unknown_user";

                return RateLimitPartition.GetSlidingWindowLimiter(userId, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                });
            });
        });

        return services;
    }
}
