using Bank.Domain.Entities;

namespace Bank.Infrastructure.Auth;

public record RefreshTokenResult(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTimeOffset? ExpiresAt = null,
    string? Error = null,
    bool ReuseDetected = false);

public interface IRefreshTokenService
{
    Task<(string RawToken, RefreshToken Entity)> CreateRefreshTokenAsync(
        Guid userId,
        Guid? familyId = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    Task<RefreshTokenResult> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    Task<bool> RevokeFamilyAsync(
        Guid familyId,
        string? revokedByIp = null,
        string reason = "MANUAL_REVOCATION",
        CancellationToken ct = default);

    Task<bool> RevokeTokenAsync(
        string rawRefreshToken,
        string? revokedByIp = null,
        CancellationToken ct = default);
}
