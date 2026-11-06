using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bank.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? principal.FindFirstValue("sub");

        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static Guid? GetCustomerId(this ClaimsPrincipal principal)
    {
        var customerId = principal.FindFirstValue("customer_id")
                         ?? principal.FindFirstValue("customerId");

        return Guid.TryParse(customerId, out var id) ? id : null;
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email)
               ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email);
    }

    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role)
            .Concat(principal.FindAll("role"))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Admin");
    }

    public static bool IsAuditor(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Auditor");
    }

    public static bool IsCustomer(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Customer");
    }
}
