using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bank.Application.Idempotency;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bank.Api.Filters;

public class IdempotencyEndpointFilter : IEndpointFilter
{
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<IdempotencyEndpointFilter> _logger;

    public IdempotencyEndpointFilter(IIdempotencyService idempotencyService, ILogger<IdempotencyEndpointFilter> logger)
    {
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var headers = httpContext.Request.Headers;

        if (!headers.TryGetValue("Idempotency-Key", out var keyValue) || string.IsNullOrWhiteSpace(keyValue))
        {
            return Results.Json(new ProblemDetails
            {
                Title = "Idempotency Key Required",
                Detail = "This financial endpoint requires an Idempotency-Key header.",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errorCode"] = "IDEMPOTENCY_KEY_REQUIRED" }
            }, statusCode: StatusCodes.Status400BadRequest);
        }

        string idempotencyKey = keyValue.ToString();

        // Extract user ID
        Guid userId = Guid.Empty;
        var subClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value;

        if (subClaim != null && Guid.TryParse(subClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        // Enable stream buffering to read request payload
        httpContext.Request.EnableBuffering();
        if (httpContext.Request.Body.CanSeek)
        {
            httpContext.Request.Body.Position = 0;
        }

        string requestBody = string.Empty;
        using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync(httpContext.RequestAborted);
            if (httpContext.Request.Body.CanSeek)
            {
                httpContext.Request.Body.Position = 0;
            }
        }

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            var bodyArg = context.Arguments.FirstOrDefault(a =>
                a != null &&
                a is not ClaimsPrincipal &&
                a is not HttpContext &&
                a is not CancellationToken &&
                !a.GetType().IsInterface);

            if (bodyArg != null)
            {
                requestBody = JsonSerializer.Serialize(bodyArg);
            }
        }

        string endpoint = $"{httpContext.Request.Method}:{httpContext.Request.Path}";
        var checkResult = await _idempotencyService.CheckOrAcquireAsync(
            userId,
            idempotencyKey,
            endpoint,
            requestBody,
            httpContext.RequestAborted);

        if (checkResult.Status == IdempotencyStatus.PayloadMismatch)
        {
            return Results.Json(new ProblemDetails
            {
                Title = "Idempotency Key Reused",
                Detail = "Idempotency-Key reused with a different request payload.",
                Status = StatusCodes.Status422UnprocessableEntity,
                Extensions = { ["errorCode"] = "IDEMPOTENCY_KEY_REUSED" }
            }, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        if (checkResult.Status == IdempotencyStatus.Conflict)
        {
            httpContext.Response.Headers.Append("Retry-After", "5");
            return Results.Json(new ProblemDetails
            {
                Title = "Request In Progress",
                Detail = "A request with this idempotency key is currently being processed.",
                Status = StatusCodes.Status409Conflict,
                Extensions = { ["errorCode"] = "REQUEST_IN_PROGRESS" }
            }, statusCode: StatusCodes.Status409Conflict);
        }

        if (checkResult.Status == IdempotencyStatus.Replay)
        {
            httpContext.Response.Headers.Append("Idempotent-Replayed", "true");
            var json = checkResult.CachedResponseBody ?? "{}";
            int statusCode = checkResult.CachedStatusCode ?? StatusCodes.Status200OK;
            return Results.Content(json, "application/json", Encoding.UTF8, statusCode);
        }

        // Store RecordId in HttpContext.Items so endpoint handler can access it
        httpContext.Items["IdempotencyRecordId"] = checkResult.RecordId;

        // Execute downstream handler
        var result = await next(context);

        // Store cached response on completion
        try
        {
            int status = StatusCodes.Status200OK;
            string bodyJson = "{}";

            if (result is IStatusCodeHttpResult statusResult && statusResult.StatusCode.HasValue)
            {
                status = statusResult.StatusCode.Value;
            }

            if (result is IValueHttpResult valueResult && valueResult.Value != null)
            {
                bodyJson = JsonSerializer.Serialize(valueResult.Value);
            }

            if (status < 500)
            {
                Guid? txnId = null;
                if (httpContext.Items.TryGetValue("TransactionId", out var tidObj) && tidObj is Guid tid)
                {
                    txnId = tid;
                }

                await _idempotencyService.CompleteAsync(
                    checkResult.RecordId,
                    status,
                    bodyJson,
                    txnId,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete idempotency record {RecordId}: {Message}", checkResult.RecordId, ex.Message);
        }

        return result;
    }
}
