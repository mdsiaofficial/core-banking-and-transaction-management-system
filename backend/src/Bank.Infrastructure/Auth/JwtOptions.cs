namespace Bank.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Bank.Api";
    public string Audience { get; set; } = "Bank.Client";
    public string SecretKey { get; set; } = "SuperSecureBankingSystemSecretKeyMustBeAtLeast32BytesLong!";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
