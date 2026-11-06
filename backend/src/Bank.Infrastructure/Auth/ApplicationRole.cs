namespace Bank.Infrastructure.Auth;

using Microsoft.AspNetCore.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
    public const string Auditor = "Auditor";

    public ApplicationRole() : base()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}
