namespace Bank.Domain.Entities;

public class ReconciliationRun
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
    public string Status { get; set; } = "RUNNING"; // RUNNING, COMPLETED, FAILED
    public int AccountsChecked { get; set; }
    public int DiscrepanciesFound { get; set; }

    public ICollection<ReconciliationFinding> Findings { get; set; } = new List<ReconciliationFinding>();
}
