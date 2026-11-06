using Bank.Api.Security;
using Bank.Application.Audit;
using Bank.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Bank.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/admin/audit-logs")
            .WithTags("Audit Logs")
            .RequireAuthorization(AuthorizationPolicies.AdminOrAuditorPolicy);

        group.MapGet("/", SearchAuditLogsAsync)
            .WithName("SearchAuditLogs")
            .WithSummary("Query immutable audit trail records");

        return routes;
    }

    private static async Task<IResult> SearchAuditLogsAsync(
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? actorRole,
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] string? action,
        [FromQuery] AuditOutcome? outcome,
        [FromQuery] string? correlationId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        [FromServices] IAuditService? auditService = null)
    {
        if (auditService == null)
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        var query = new AuditLogQuery(
            ActorUserId: actorUserId,
            ActorRole: actorRole,
            EntityType: entityType,
            EntityId: entityId,
            Action: action,
            Outcome: outcome,
            CorrelationId: correlationId,
            From: from,
            To: to,
            Limit: limit,
            Cursor: cursor);

        var results = await auditService.SearchAsync(query);

        string? nextCursor = null;
        if (results.Count >= limit && results.Count > 0)
        {
            nextCursor = results[^1].OccurredAt.ToString("O");
        }

        return Results.Ok(new
        {
            items = results,
            count = results.Count,
            nextCursor
        });
    }
}
