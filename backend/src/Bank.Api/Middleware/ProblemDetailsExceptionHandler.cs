using System.Diagnostics;
using Bank.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bank.Api.Middleware;

public class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;

    public ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, title, errorCode) = exception switch
        {
            InsufficientFundsException => (StatusCodes.Status422UnprocessableEntity, "Insufficient Funds", "INSUFFICIENT_FUNDS"),
            AccountFrozenException => (StatusCodes.Status403Forbidden, "Account Frozen", "ACCOUNT_FROZEN"),
            AccountClosedException => (StatusCodes.Status400BadRequest, "Account Closed", "ACCOUNT_CLOSED"),
            CurrencyMismatchException => (StatusCodes.Status400BadRequest, "Currency Mismatch", "CURRENCY_MISMATCH"),
            DailyLimitExceededException => (StatusCodes.Status422UnprocessableEntity, "Daily Limit Exceeded", "DAILY_LIMIT_EXCEEDED"),
            InvalidAccountNumberException => (StatusCodes.Status400BadRequest, "Invalid Account Number", "INVALID_ACCOUNT_NUMBER"),
            InvalidMoneyException => (StatusCodes.Status400BadRequest, "Invalid Amount", "INVALID_AMOUNT"),
            UnbalancedTransactionException => (StatusCodes.Status500InternalServerError, "Ledger Invariant Violation", "UNBALANCED_TRANSACTION"),
            DomainException de => (StatusCodes.Status400BadRequest, "Business Rule Violation", de.ErrorCode),
            ArgumentException ae => (StatusCodes.Status400BadRequest, "Bad Request", "INVALID_ARGUMENT"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "SERVER_ERROR")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["errorCode"] = errorCode,
                ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier
            }
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
