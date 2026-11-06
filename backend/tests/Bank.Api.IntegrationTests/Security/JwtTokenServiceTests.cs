using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Bank.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace Bank.Api.IntegrationTests.Security;

public class JwtTokenServiceTests
{
    private readonly JwtOptions _options = new()
    {
        Issuer = "Bank.Api.Test",
        Audience = "Bank.Client.Test",
        SecretKey = "TestSecretKeyMustBeAtLeast32BytesLongForHmacSha256Security!",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 7
    };

    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        _service = new JwtTokenService(Options.Create(_options));
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeMinimalNecessaryClaims()
    {
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "customer@example.com",
            CustomerId = customerId
        };
        var roles = new List<string> { "Customer" };

        var (token, expiresAt, jti) = _service.GenerateAccessToken(user, roles);

        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        jti.Should().NotBeEmpty();

        var principal = _service.GetPrincipalFromToken(token);
        principal.Should().NotBeNull();

        var subClaim = principal!.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                       ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        subClaim.Should().Be(userId.ToString());

        var customerIdClaim = principal.FindFirst("customer_id")?.Value;
        customerIdClaim.Should().Be(customerId.ToString());

        var roleClaims = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
        roleClaims.Should().Contain("Customer");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldProduceCryptographicallyRandomTokenAndSha256Hash()
    {
        var (rawToken1, hash1) = _service.GenerateRefreshToken();
        var (rawToken2, hash2) = _service.GenerateRefreshToken();

        rawToken1.Should().NotBeNullOrWhiteSpace();
        rawToken2.Should().NotBeNullOrWhiteSpace();
        rawToken1.Should().NotBe(rawToken2);

        hash1.Length.Should().Be(64); // SHA-256 hex string length
        hash2.Length.Should().Be(64);

        _service.HashToken(rawToken1).Should().Be(hash1);
        _service.HashToken(rawToken2).Should().Be(hash2);
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ShouldReturnNull()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com" };
        var (token, _, _) = _service.GenerateAccessToken(user, new[] { "Customer" });

        var tamperedToken = token[..^5] + "XXXXX";
        var principal = _service.GetPrincipalFromToken(tamperedToken);

        principal.Should().BeNull();
    }
}
