using Bank.Application.Reconciliation;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bank.Infrastructure.Services;

public class ReconciliationService : IReconciliationService
{
    private readonly BankDbContext _db;
    private readonly ILogger<ReconciliationService> _logger;

    public ReconciliationService(BankDbContext db, ILogger<ReconciliationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReconciliationRunDto> RunReconciliationAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting automated balance reconciliation run...");

        var run = new ReconciliationRun
        {
            Id = Guid.CreateVersion7(),
            StartedAt = DateTimeOffset.UtcNow,
            Status = "RUNNING"
        };
        _db.ReconciliationRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        int accountsChecked = 0;
        int discrepanciesFound = 0;
        var findings = new List<ReconciliationFinding>();

        var accounts = await _db.Accounts.AsNoTracking().ToListAsync(ct);

        // 1. Check each account balance vs computed ledger entries
        foreach (var account in accounts)
        {
            accountsChecked++;

            var entries = await _db.LedgerEntries
                .AsNoTracking()
                .Where(e => e.AccountId == account.Id)
                .OrderBy(e => e.AccountSeq)
                .ToListAsync(ct);

            decimal computedBalance = 0m;
            foreach (var e in entries)
            {
                if (account.LedgerClass == LedgerClass.Asset || account.LedgerClass == LedgerClass.Expense)
                {
                    if (e.Direction == EntryDirection.Debit)
                        computedBalance += e.Amount;
                    else
                        computedBalance -= e.Amount;
                }
                else
                {
                    if (e.Direction == EntryDirection.Credit)
                        computedBalance += e.Amount;
                    else
                        computedBalance -= e.Amount;
                }
            }

            // For internal accounts with initial baseline balance (e.g. Cash vault 100M baseline)
            // if last_entry_seq == 0 and computedBalance == 0, expected is account.Balance
            decimal expected = computedBalance;
            if (account.Kind == AccountKind.Internal && entries.Count == 0)
            {
                expected = account.Balance;
            }

            if (account.Kind == AccountKind.Customer && computedBalance != account.Balance)
            {
                discrepanciesFound++;
                var finding = new ReconciliationFinding
                {
                    Id = Guid.CreateVersion7(),
                    RunId = run.Id,
                    AccountId = account.Id,
                    ExpectedBalance = computedBalance,
                    ActualBalance = account.Balance,
                    Currency = account.Currency,
                    Severity = "CRITICAL",
                    Description = $"Balance discrepancy for account {account.AccountNumber}: expected {computedBalance} {account.Currency}, found {account.Balance} {account.Currency}. Diff: {Math.Abs(computedBalance - account.Balance)}"
                };
                findings.Add(finding);
                _db.ReconciliationFindings.Add(finding);
            }

            // 2. Check per-account gapless sequence
            if (entries.Count > 0)
            {
                long expectedSeq = 1;
                foreach (var e in entries)
                {
                    if (e.AccountSeq != expectedSeq)
                    {
                        discrepanciesFound++;
                        var seqFinding = new ReconciliationFinding
                        {
                            Id = Guid.CreateVersion7(),
                            RunId = run.Id,
                            AccountId = account.Id,
                            ExpectedBalance = computedBalance,
                            ActualBalance = account.Balance,
                            Currency = account.Currency,
                            Severity = "CRITICAL",
                            Description = $"Sequence gap in account {account.AccountNumber}: expected seq {expectedSeq}, found {e.AccountSeq} on entry {e.Id}."
                        };
                        findings.Add(seqFinding);
                        _db.ReconciliationFindings.Add(seqFinding);
                        break;
                    }
                    expectedSeq++;
                }
            }
        }

        // 3. Global debits = credits check
        decimal globalDebits = await _db.LedgerEntries
            .Where(e => e.Direction == EntryDirection.Debit)
            .SumAsync(e => e.Amount, ct);

        decimal globalCredits = await _db.LedgerEntries
            .Where(e => e.Direction == EntryDirection.Credit)
            .SumAsync(e => e.Amount, ct);

        if (globalDebits != globalCredits)
        {
            discrepanciesFound++;
            var globalFinding = new ReconciliationFinding
            {
                Id = Guid.CreateVersion7(),
                RunId = run.Id,
                AccountId = accounts.FirstOrDefault()?.Id ?? Guid.Empty,
                ExpectedBalance = globalCredits,
                ActualBalance = globalDebits,
                Currency = "BDT",
                Severity = "CRITICAL",
                Description = $"Global ledger imbalance: total debits ({globalDebits}) != total credits ({globalCredits}). Diff: {Math.Abs(globalDebits - globalCredits)}."
            };
            findings.Add(globalFinding);
            _db.ReconciliationFindings.Add(globalFinding);
        }

        run.FinishedAt = DateTimeOffset.UtcNow;
        run.AccountsChecked = accountsChecked;
        run.DiscrepanciesFound = discrepanciesFound;
        run.Status = discrepanciesFound == 0 ? "COMPLETED" : "DISCREPANCIES_DETECTED";

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Reconciliation run {RunId} finished. Status: {Status}, Accounts checked: {Count}, Discrepancies: {Discrepancies}",
            run.Id, run.Status, accountsChecked, discrepanciesFound);

        return new ReconciliationRunDto(
            run.Id,
            run.StartedAt,
            run.FinishedAt,
            run.Status,
            run.AccountsChecked,
            run.DiscrepanciesFound,
            findings.Select(f => new ReconciliationFindingDto(
                f.Id,
                f.RunId,
                f.AccountId,
                accounts.FirstOrDefault(a => a.Id == f.AccountId)?.AccountNumber ?? string.Empty,
                f.ExpectedBalance,
                f.ActualBalance,
                f.Currency,
                f.Severity,
                f.Description,
                f.ResolvedAt)).ToList());
    }

    public async Task<IReadOnlyList<ReconciliationRunDto>> GetRunsAsync(int limit = 20, CancellationToken ct = default)
    {
        int clampedLimit = Math.Clamp(limit, 1, 100);
        var runs = await _db.ReconciliationRuns
            .AsNoTracking()
            .Include(r => r.Findings)
            .OrderByDescending(r => r.StartedAt)
            .Take(clampedLimit)
            .ToListAsync(ct);

        return runs.Select(MapToDto).ToList();
    }

    public async Task<ReconciliationRunDto?> GetRunByIdAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await _db.ReconciliationRuns
            .AsNoTracking()
            .Include(r => r.Findings)
            .ThenInclude(f => f.Account)
            .FirstOrDefaultAsync(r => r.Id == runId, ct);

        return run is null ? null : MapToDto(run);
    }

    private static ReconciliationRunDto MapToDto(ReconciliationRun r) => new(
        r.Id,
        r.StartedAt,
        r.FinishedAt,
        r.Status,
        r.AccountsChecked,
        r.DiscrepanciesFound,
        r.Findings.Select(f => new ReconciliationFindingDto(
            f.Id,
            f.RunId,
            f.AccountId,
            f.Account?.AccountNumber ?? string.Empty,
            f.ExpectedBalance,
            f.ActualBalance,
            f.Currency,
            f.Severity,
            f.Description,
            f.ResolvedAt)).ToList());
}
