using Bank.Domain.Entities;
using Bank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bank.Infrastructure.Auth;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly BankDbContext _db;

    public RefreshTokenRepository(BankDbContext db)
    {
        _db = db;
    }

    public async Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _db.RefreshTokens.AddAsync(token, ct);
        return token;
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        return await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);
    }

    public async Task<List<RefreshToken>> GetByFamilyIdAsync(Guid familyId, CancellationToken ct = default)
    {
        return await _db.RefreshTokens
            .Where(r => r.FamilyId == familyId)
            .ToListAsync(ct);
    }

    public async Task RevokeFamilyAsync(Guid familyId, string? revokedByIp, CancellationToken ct = default)
    {
        var tokens = await _db.RefreshTokens
            .Where(r => r.FamilyId == familyId && r.RevokedAt == null)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = revokedByIp;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
