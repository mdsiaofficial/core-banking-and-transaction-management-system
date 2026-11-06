namespace Bank.Application.Audit;

public interface IAuditService
{
    Task LogAsync(AuditLogEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogDto>> SearchAsync(AuditLogQuery query, CancellationToken ct = default);
}
