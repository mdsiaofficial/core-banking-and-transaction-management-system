using System.Security.Claims;
using Bank.Infrastructure.Auth;

namespace Bank.Infrastructure.Auth;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt, Guid Jti) GenerateAccessToken(ApplicationUser user, IList<string> roles);
    (string RawToken, string TokenHash) GenerateRefreshToken();
    string HashToken(string rawToken);
    ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true);
}
