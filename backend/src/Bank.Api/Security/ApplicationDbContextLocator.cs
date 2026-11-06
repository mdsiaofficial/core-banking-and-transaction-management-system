using System.Security.Claims;
using Bank.Application.Accounts;
using Bank.Infrastructure.Auth;
using Bank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bank.Api.Security;

public class ApplicationDbContextLocator
{
    private readonly BankDbContext _db;

    public ApplicationDbContextLocator(BankDbContext db)
    {
        _db = db;
    }

    public async Task<Guid?> ResolveCustomerIdAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return null;
        }

        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        return customer?.Id;
    }

    public async Task<bool> CanAccessAccountAsync(ClaimsPrincipal user, AccountDto account, CancellationToken ct = default)
    {
        if (user.IsInRole(ApplicationRole.Admin) || user.IsInRole(ApplicationRole.Auditor))
        {
            return true;
        }

        var customerId = await ResolveCustomerIdAsync(user, ct);
        return customerId.HasValue && account.CustomerId == customerId.Value;
    }
}
