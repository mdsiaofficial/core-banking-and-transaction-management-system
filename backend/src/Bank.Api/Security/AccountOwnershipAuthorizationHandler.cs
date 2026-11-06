using Bank.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Bank.Api.Security;

public class AccountOwnershipRequirement : IAuthorizationRequirement
{
}

public class AccountOwnershipAuthorizationHandler : AuthorizationHandler<AccountOwnershipRequirement, Account>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccountOwnershipRequirement requirement,
        Account resource)
    {
        var user = context.User;

        // Admins and Auditors have full view/operate rights across all accounts
        if (user.IsInRole("Admin") || user.IsInRole("Auditor"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Customers can ONLY access accounts where CustomerId matches their customer_id claim
        var userCustomerId = user.GetCustomerId();
        if (userCustomerId.HasValue && resource.CustomerId.HasValue && userCustomerId.Value == resource.CustomerId.Value)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Otherwise deny (IDOR / BOLA prevention)
        context.Fail();
        return Task.CompletedTask;
    }
}
