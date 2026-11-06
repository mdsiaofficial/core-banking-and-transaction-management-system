namespace Bank.Domain.Entities;

public class ReconciliationFinding
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RunId { get; set; }
    public Guid AccountId { get; set; }
    public decimal ExpectedBalance { get; set; }
    public decimal ActualBalance { get; set; }
    public string Currency { get; set; } = "BDT";
    public string Severity { get; set; } = "CRITICAL"; // CRITICAL, WARNING
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset? ResolvedAt { get; set; }

    public ReconciliationRun? Run { get; set; }
    public Account? Account { get; set; }
}
