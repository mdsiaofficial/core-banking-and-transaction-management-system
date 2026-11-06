using Bank.Domain.Entities;

namespace Bank.Infrastructure.Auth;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task<List<RefreshToken>> GetByFamilyIdAsync(Guid familyId, CancellationToken ct = default);
    Task RevokeFamilyAsync(Guid familyId, string? revokedByIp, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
