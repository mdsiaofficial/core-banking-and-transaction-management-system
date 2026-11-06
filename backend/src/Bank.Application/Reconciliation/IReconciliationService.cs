namespace Bank.Application.Reconciliation;

public record ReconciliationFindingDto(
    Guid Id,
    Guid RunId,
    Guid AccountId,
    string AccountNumber,
    decimal ExpectedBalance,
    decimal ActualBalance,
    string Currency,
    string Severity,
    string Description,
    DateTimeOffset? ResolvedAt);

public record ReconciliationRunDto(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string Status,
    int AccountsChecked,
    int DiscrepanciesFound,
    IReadOnlyList<ReconciliationFindingDto> Findings);

public interface IReconciliationService
{
    Task<ReconciliationRunDto> RunReconciliationAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReconciliationRunDto>> GetRunsAsync(int limit = 20, CancellationToken ct = default);
    Task<ReconciliationRunDto?> GetRunByIdAsync(Guid runId, CancellationToken ct = default);
}
