using Bank.Application.Audit;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank.Infrastructure.Auth;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _repository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _options;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        IRefreshTokenRepository repository,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> options,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        ILogger<RefreshTokenService> logger)
    {
        _repository = repository;
        _jwtTokenService = jwtTokenService;
        _options = options.Value;
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<(string RawToken, RefreshToken Entity)> CreateRefreshTokenAsync(
        Guid userId,
        Guid? familyId = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        var (rawToken, tokenHash) = _jwtTokenService.GenerateRefreshToken();
        var finalFamilyId = familyId ?? Guid.CreateVersion7();

        var tokenEntity = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = finalFamilyId,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenLifetimeDays),
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

        await _repository.AddAsync(tokenEntity, ct);
        await _repository.SaveChangesAsync(ct);

        return (rawToken, tokenEntity);
    }

    public async Task<RefreshTokenResult> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return new RefreshTokenResult(false, Error: "Refresh token is required.");
        }

        var hash = _jwtTokenService.HashToken(rawRefreshToken);
        var token = await _repository.GetByHashAsync(hash, ct);

        if (token == null)
        {
            _logger.LogWarning("Refresh token hash not found in database.");
            await _auditService.LogAsync(new AuditLogEntry(
                Action: "AUTH_REFRESH_FAILED",
                EntityType: "RefreshToken",
                EntityId: null,
                ActorIp: ipAddress,
                UserAgent: userAgent,
                Outcome: AuditOutcome.Denied,
                Metadata: "{\"reason\":\"Token not found\"}"
            ), ct);

            return new RefreshTokenResult(false, Error: "Invalid refresh token.");
        }

        // REUSE DETECTION: If token was already revoked or replaced, an attacker or compromised client is reusing it
        if (token.RevokedAt != null || token.ReplacedById != null)
        {
            _logger.LogCritical("Security Alert: Refresh token reuse detected for user {UserId}, family {FamilyId}. Revoking entire token family immediately.",
                token.UserId, token.FamilyId);

            // Invalidate the entire family
            await _repository.RevokeFamilyAsync(token.FamilyId, ipAddress, ct);

            await _auditService.LogAsync(new AuditLogEntry(
                Action: "AUTH_REFRESH_TOKEN_REUSE_DETECTED",
                EntityType: "RefreshTokenFamily",
                EntityId: token.FamilyId.ToString(),
                ActorUserId: token.UserId,
                ActorIp: ipAddress,
                UserAgent: userAgent,
                Outcome: AuditOutcome.Denied,
                Metadata: $"{{\"family_id\":\"{token.FamilyId}\",\"compromised_token_id\":\"{token.Id}\"}}"
            ), ct);

            return new RefreshTokenResult(
                Success: false,
                Error: "Refresh token reuse detected. All tokens in this family have been revoked.",
                ReuseDetected: true);
        }

        // Check token expiration
        if (token.IsExpired)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = ipAddress;
            await _repository.SaveChangesAsync(ct);

            await _auditService.LogAsync(new AuditLogEntry(
                Action: "AUTH_REFRESH_EXPIRED",
                EntityType: "RefreshToken",
                EntityId: token.Id.ToString(),
                ActorUserId: token.UserId,
                ActorIp: ipAddress,
                UserAgent: userAgent,
                Outcome: AuditOutcome.Denied
            ), ct);

            return new RefreshTokenResult(false, Error: "Refresh token has expired.");
        }

        // Token is active and valid: Perform rotation
        var (newRawToken, newTokenHash) = _jwtTokenService.GenerateRefreshToken();

        var newToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = token.UserId,
            TokenHash = newTokenHash,
            FamilyId = token.FamilyId, // Preserve the family ID
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenLifetimeDays),
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

        await _repository.AddAsync(newToken, ct);

        // Mark existing token as revoked and replaced by new token
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedById = newToken.Id;

        await _repository.SaveChangesAsync(ct);

        // Fetch user and generate new access token
        var user = await _userManager.FindByIdAsync(token.UserId.ToString());
        if (user == null || await _userManager.IsLockedOutAsync(user))
        {
            return new RefreshTokenResult(false, Error: "User account is unavailable or locked.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, accessExpiresAt, _) = _jwtTokenService.GenerateAccessToken(user, roles);

        await _auditService.LogAsync(new AuditLogEntry(
            Action: "AUTH_TOKEN_ROTATED",
            EntityType: "RefreshToken",
            EntityId: newToken.Id.ToString(),
            ActorUserId: user.Id,
            ActorRole: roles.FirstOrDefault() ?? "Customer",
            ActorIp: ipAddress,
            UserAgent: userAgent,
            Outcome: AuditOutcome.Success,
            Metadata: $"{{\"family_id\":\"{token.FamilyId}\",\"previous_token_id\":\"{token.Id}\"}}"
        ), ct);

        return new RefreshTokenResult(
            Success: true,
            AccessToken: accessToken,
            RefreshToken: newRawToken,
            ExpiresAt: accessExpiresAt);
    }

    public async Task<bool> RevokeFamilyAsync(
        Guid familyId,
        string? revokedByIp = null,
        string reason = "MANUAL_REVOCATION",
        CancellationToken ct = default)
    {
        await _repository.RevokeFamilyAsync(familyId, revokedByIp, ct);

        await _auditService.LogAsync(new AuditLogEntry(
            Action: "AUTH_FAMILY_REVOKED",
            EntityType: "RefreshTokenFamily",
            EntityId: familyId.ToString(),
            ActorIp: revokedByIp,
            Outcome: AuditOutcome.Success,
            Metadata: $"{{\"reason\":\"{reason}\"}}"
        ), ct);

        return true;
    }

    public async Task<bool> RevokeTokenAsync(
        string rawRefreshToken,
        string? revokedByIp = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return false;
        }

        var hash = _jwtTokenService.HashToken(rawRefreshToken);
        var token = await _repository.GetByHashAsync(hash, ct);
        if (token == null)
        {
            return false;
        }

        // On logout, revoking the entire family invalidates the current session chain
        await _repository.RevokeFamilyAsync(token.FamilyId, revokedByIp, ct);

        await _auditService.LogAsync(new AuditLogEntry(
            Action: "AUTH_LOGOUT",
            EntityType: "RefreshToken",
            EntityId: token.Id.ToString(),
            ActorUserId: token.UserId,
            ActorIp: revokedByIp,
            Outcome: AuditOutcome.Success,
            Metadata: $"{{\"family_id\":\"{token.FamilyId}\"}}"
        ), ct);

        return true;
    }
}
