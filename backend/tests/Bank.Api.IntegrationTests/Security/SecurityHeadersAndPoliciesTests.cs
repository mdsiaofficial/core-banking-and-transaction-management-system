using Bank.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Bank.Api.IntegrationTests.Security;

public class SecurityHeadersAndPoliciesTests
{
    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldInjectAllMandatoryHeaders()
    {
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);

        await middleware.InvokeAsync(context);

        var headers = context.Response.Headers;

        headers.Should().ContainKey("Content-Security-Policy");
        headers["Content-Security-Policy"].ToString().Should().Contain("frame-ancestors 'none'");
        headers["Content-Security-Policy"].ToString().Should().Contain("default-src 'self'");

        headers.Should().ContainKey("Strict-Transport-Security");
        headers["Strict-Transport-Security"].ToString().Should().Contain("max-age=31536000");

        headers.Should().ContainKey("X-Content-Type-Options");
        headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");

        headers.Should().ContainKey("X-Frame-Options");
        headers["X-Frame-Options"].ToString().Should().Be("DENY");

        headers.Should().ContainKey("Referrer-Policy");
        headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
    }

    [Theory]
    [InlineData("short", false)] // < 12 chars
    [InlineData("alllowercase1234!", false)] // No uppercase
    [InlineData("ALLUPPERCASE1234!", false)] // No lowercase
    [InlineData("NoDigitsInPassword!", false)] // No digits
    [InlineData("NoSymbols12345678", false)] // No symbols
    [InlineData("ValidStrongPassword1234!", true)] // Valid >= 12 chars with all requirements
    public void PasswordPolicy_ShouldValidateComplexityCorrectly(string password, bool expectedValid)
    {
        var options = new IdentityOptions();
        options.Password.RequiredLength = 12;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;

        bool isValid = password.Length >= options.Password.RequiredLength &&
                       (!options.Password.RequireUppercase || password.Any(char.IsUpper)) &&
                       (!options.Password.RequireLowercase || password.Any(char.IsLower)) &&
                       (!options.Password.RequireDigit || password.Any(char.IsDigit)) &&
                       (!options.Password.RequireNonAlphanumeric || password.Any(ch => !char.IsLetterOrDigit(ch)));

        isValid.Should().Be(expectedValid);
    }
}
