using Bank.Application.Audit;
using Bank.Domain.Entities;
using Bank.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bank.Api.IntegrationTests.Security;

public class RefreshTokenReuseDetectionTests
{
    private readonly InMemoryRefreshTokenRepository _repository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly RefreshTokenService _service;
    private readonly ApplicationUser _user;

    public RefreshTokenReuseDetectionTests()
    {
        _repository = new InMemoryRefreshTokenRepository();

        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = "SuperSecureKeyForRefreshTokenRotationTestingMustBe32Chars!",
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 7
        });

        _jwtTokenService = new JwtTokenService(jwtOptions);

        var userStore = new FakeUserStore();
        var userManager = new FakeUserManager(userStore);

        _user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser@bank.local",
            Email = "testuser@bank.local"
        };
        userStore.Users.Add(_user);

        var auditService = new FakeAuditService();

        _service = new RefreshTokenService(
            _repository,
            _jwtTokenService,
            jwtOptions,
            userManager,
            auditService,
            NullLogger<RefreshTokenService>.Instance);
    }

    [Fact]
    public async Task RotateTokenAsync_WhenTokenIsActive_ShouldRotateSuccessfullyPreservingFamily()
    {
        // 1. Issue initial token
        var (rawToken1, entity1) = await _service.CreateRefreshTokenAsync(_user.Id, null, "127.0.0.1", "TestClient");

        entity1.IsActive.Should().BeTrue();
        entity1.FamilyId.Should().NotBeEmpty();

        // 2. Rotate token 1
        var result = await _service.RotateRefreshTokenAsync(rawToken1, "127.0.0.1", "TestClient");

        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBe(rawToken1);

        // 3. Verify entity 1 state in repository
        var updatedEntity1 = await _repository.GetByHashAsync(entity1.TokenHash);
        updatedEntity1!.IsRevoked.Should().BeTrue();
        updatedEntity1.ReplacedById.Should().NotBeNull();

        // 4. Verify new token entity has same FamilyId
        var newHash = _jwtTokenService.HashToken(result.RefreshToken!);
        var newEntity = await _repository.GetByHashAsync(newHash);
        newEntity.Should().NotBeNull();
        newEntity!.FamilyId.Should().Be(entity1.FamilyId);
        updatedEntity1.ReplacedById.Should().Be(newEntity.Id);
    }

    [Fact]
    public async Task RotateTokenAsync_WhenReusedTokenIsPresented_ShouldDetectReuseAndRevokeEntireFamily()
    {
        // 1. Issue initial token 1
        var (rawToken1, entity1) = await _service.CreateRefreshTokenAsync(_user.Id, null, "127.0.0.1", "TestClient");
        var familyId = entity1.FamilyId;

        // 2. Legitimate user rotates token 1 -> gets token 2
        var rotateResult1 = await _service.RotateRefreshTokenAsync(rawToken1, "127.0.0.1", "LegitimateClient");
        rotateResult1.Success.Should().BeTrue();
        var rawToken2 = rotateResult1.RefreshToken!;

        // 3. Legitimate user rotates token 2 -> gets token 3
        var rotateResult2 = await _service.RotateRefreshTokenAsync(rawToken2, "127.0.0.1", "LegitimateClient");
        rotateResult2.Success.Should().BeTrue();
        var rawToken3 = rotateResult2.RefreshToken!;

        // Token 3 should be active right now
        var token3Hash = _jwtTokenService.HashToken(rawToken3);
        var token3Entity = await _repository.GetByHashAsync(token3Hash);
        token3Entity!.IsActive.Should().BeTrue();

        // 4. Attacker attempts to replay token 1 (which was already rotated/replaced)
        var attackResult = await _service.RotateRefreshTokenAsync(rawToken1, "192.168.1.100", "AttackerClient");

        // Verify reuse detected and attack rejected
        attackResult.Success.Should().BeFalse();
        attackResult.ReuseDetected.Should().BeTrue();
        attackResult.Error.Should().Contain("reuse detected");

        // 5. Verify the ENTIRE token family (including token 3) is now revoked!
        var familyTokens = await _repository.GetByFamilyIdAsync(familyId);
        familyTokens.Should().NotBeEmpty();
        familyTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());

        // 6. Verify subsequent attempt to use token 3 also fails
        var legitAttemptWithToken3 = await _service.RotateRefreshTokenAsync(rawToken3, "127.0.0.1", "LegitimateClient");
        legitAttemptWithToken3.Success.Should().BeFalse();
        legitAttemptWithToken3.ReuseDetected.Should().BeTrue();
    }

    private sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly List<RefreshToken> _tokens = new();

        public Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken ct = default)
        {
            _tokens.Add(token);
            return Task.FromResult(token);
        }

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        {
            return Task.FromResult(_tokens.FirstOrDefault(t => t.TokenHash == tokenHash));
        }

        public Task<List<RefreshToken>> GetByFamilyIdAsync(Guid familyId, CancellationToken ct = default)
        {
            return Task.FromResult(_tokens.Where(t => t.FamilyId == familyId).ToList());
        }

        public Task RevokeFamilyAsync(Guid familyId, string? revokedByIp, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var token in _tokens.Where(t => t.FamilyId == familyId && t.RevokedAt == null))
            {
                token.RevokedAt = now;
                token.RevokedByIp = revokedByIp;
            }
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeAuditService : IAuditService
    {
        public List<AuditLogEntry> Entries { get; } = new();

        public Task LogAsync(AuditLogEntry entry, CancellationToken ct = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditLogDto>> SearchAsync(AuditLogQuery query, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<AuditLogDto>>(new List<AuditLogDto>());
        }
    }

    private sealed class FakeUserStore : IUserStore<ApplicationUser>, IUserRoleStore<ApplicationUser>
    {
        public List<ApplicationUser> Users { get; } = new();

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            Users.Add(user);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public void Dispose() { }
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Id.ToString() == userId));

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(u => u.UserName == normalizedUserName));

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.UserName);
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

        public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult<IList<string>>(new List<string> { "Customer" });
        public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => Task.FromResult(roleName == "Customer");
        public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) => Task.FromResult<IList<ApplicationUser>>(Users);
    }

    private sealed class FakeUserManager : UserManager<ApplicationUser>
    {
        public FakeUserManager(IUserStore<ApplicationUser> store)
            : base(store, null!, null!, null!, null!, null!, null!, null!, null!)
        {
        }

        public override Task<bool> IsLockedOutAsync(ApplicationUser user) => Task.FromResult(false);
    }
}
