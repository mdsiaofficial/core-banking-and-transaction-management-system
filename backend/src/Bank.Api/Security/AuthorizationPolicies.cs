using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Bank.Api.Security;

public static class AuthorizationPolicies
{
    public const string CustomerPolicy = "CustomerOnly";
    public const string AdminPolicy = "AdminOnly";
    public const string AuditorPolicy = "AuditorOnly";
    public const string AdminOrAuditorPolicy = "AdminOrAuditor";
    public const string AccountOwnershipPolicy = "AccountOwnership";

    public static IServiceCollection AddBankAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(CustomerPolicy, policy => policy.RequireRole("Customer"))
            .AddPolicy(AdminPolicy, policy => policy.RequireRole("Admin"))
            .AddPolicy(AuditorPolicy, policy => policy.RequireRole("Auditor"))
            .AddPolicy(AdminOrAuditorPolicy, policy => policy.RequireRole("Admin", "Auditor"))
            .AddPolicy(AccountOwnershipPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new AccountOwnershipRequirement());
            });

        services.AddSingleton<IAuthorizationHandler, AccountOwnershipAuthorizationHandler>();
        services.AddScoped<ApplicationDbContextLocator>();

        return services;
    }
}
