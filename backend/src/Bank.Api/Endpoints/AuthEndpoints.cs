using Bank.Application.Audit;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Infrastructure.Auth;
using Bank.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Bank.Api.Endpoints;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    DateOnly DateOfBirth,
    string Phone,
    string? Address = null);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string? RefreshToken = null);

public record VerifyEmailRequest(string Email, string Token);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register a new retail customer account");

        group.MapPost("/verify-email", VerifyEmailAsync)
            .WithName("VerifyEmail")
            .WithSummary("Verify customer email address");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Authenticate with email and password");

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .WithSummary("Rotate refresh token and issue new access token");

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("Revoke refresh token and end session");

        return routes;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] BankDbContext db,
        [FromServices] IAuditService auditService,
        HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = "Email and password are required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Account Conflict",
                Detail = "An account with this email address already exists.",
                Status = StatusCodes.Status409Conflict,
                Extensions = { ["errorCode"] = "EMAIL_ALREADY_EXISTS" }
            });
        }

        var customerId = Guid.CreateVersion7();
        var customerNo = "CUST-" + Random.Shared.Next(100000, 999999);

        var customer = new Customer
        {
            Id = customerId,
            CustomerNo = customerNo,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone,
            AddressJson = request.Address != null ? $"{{\"address\":\"{request.Address}\"}}" : "{}",
            Status = CustomerStatus.Active,
            KycStatus = KycStatus.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = request.Email,
            Email = request.Email,
            CustomerId = customerId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        customer.UserId = user.Id;

        // Save customer first
        await db.Customers.AddAsync(customer);
        await db.SaveChangesAsync();

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            // Roll back customer if user creation fails
            db.Customers.Remove(customer);
            await db.SaveChangesAsync();

            var errors = createResult.Errors.Select(e => e.Description).ToArray();
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Registration Failed",
                Detail = string.Join("; ", errors),
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errors"] = errors, ["errorCode"] = "PASSWORD_POLICY_VIOLATION" }
            });
        }

        await userManager.AddToRoleAsync(user, ApplicationRole.Customer);

        var ip = context.Connection.RemoteIpAddress?.ToString();
        await auditService.LogAsync(new AuditLogEntry(
            Action: "AUTH_REGISTER_SUCCESS",
            EntityType: "Customer",
            EntityId: customer.Id.ToString(),
            ActorUserId: user.Id,
            ActorRole: "Customer",
            ActorIp: ip,
            Outcome: AuditOutcome.Success,
            Metadata: $"{{\"email\":\"{user.Email}\",\"customer_no\":\"{customer.CustomerNo}\"}}"
        ));

        return Results.Created($"/api/v1/customers/{customer.Id}", new
        {
            userId = user.Id,
            customerId = customer.Id,
            customerNo = customer.CustomerNo,
            email = user.Email,
            fullName = customer.FullName
        });
    }

    private static async Task<IResult> VerifyEmailAsync(
        [FromBody] VerifyEmailRequest request,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] IAuditService auditService,
        HttpContext context)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Status = StatusCodes.Status404NotFound
            });
        }

        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);

        var ip = context.Connection.RemoteIpAddress?.ToString();
        await auditService.LogAsync(new AuditLogEntry(
            Action: "AUTH_VERIFY_EMAIL_SUCCESS",
            EntityType: "User",
            EntityId: user.Id.ToString(),
            ActorUserId: user.Id,
            ActorIp: ip,
            Outcome: AuditOutcome.Success
        ));

        return Results.Ok(new { message = "Email verified successfully." });
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] IRefreshTokenService refreshTokenService,
        [FromServices] IAuditService auditService,
        HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            await auditService.LogAsync(new AuditLogEntry(
                Action: "AUTH_LOGIN_FAILED",
                EntityType: "User",
                ActorIp: ip,
                UserAgent: userAgent,
                Outcome: AuditOutcome.Denied,
                Metadata: $"{{\"email\":\"{request.Email}\",\"reason\":\"User not found\"}}"
            ));

            return Results.Json(new ProblemDetails
            {
                Title = "Authentication Failed",
                Detail = "Invalid email or password.",
                Status = StatusCodes.Status401Unauthorized,
                Extensions = { ["errorCode"] = "INVALID_CREDENTIALS" }
            }, statusCode: StatusCodes.Status401Unauthorized);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            await auditService.LogAsync(new AuditLogEntry(
                Action: "AUTH_LOGIN_LOCKED_OUT",
                EntityType: "User",
                EntityId: user.Id.ToString(),
                ActorUserId: user.Id,
                ActorIp: ip,
                UserAgent: userAgent,
                Outcome: AuditOutcome.Denied,
                Metadata: "{\"reason\":\"Account is locked out\"}"
            ));

            return Results.Json(new ProblemDetails
            {
                Title = "Account Locked",
                Detail = "Account is locked due to too many failed login attempts. Please retry later.",
                Status = StatusCodes.Status403Forbidden,
                Extensions = { ["errorCode"] = "ACCOUNT_LOCKED" }
            }, statusCode: StatusCodes.Status403Forbidden);
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await userManager.AccessFailedAsync(user);

            var isNowLocked = await userManager.IsLockedOutAsync(user);
            if (isNowLocked)
            {
                await auditService.LogAsync(new AuditLogEntry(
                    Action: "AUTH_ACCOUNT_LOCKOUT_TRIGGERED",
                    EntityType: "User",
                    EntityId: user.Id.ToString(),
                    ActorUserId: user.Id,
                    ActorIp: ip,
                    UserAgent: userAgent,
                    Outcome: AuditOutcome.Denied
                ));
            }
            else
            {
                await auditService.LogAsync(new AuditLogEntry(
                    Action: "AUTH_LOGIN_FAILED",
                    EntityType: "User",
                    EntityId: user.Id.ToString(),
                    ActorUserId: user.Id,
                    ActorIp: ip,
                    UserAgent: userAgent,
                    Outcome: AuditOutcome.Denied
                ));
            }

            return Results.Json(new ProblemDetails
            {
                Title = "Authentication Failed",
                Detail = "Invalid email or password.",
                Status = StatusCodes.Status401Unauthorized,
                Extensions = { ["errorCode"] = "INVALID_CREDENTIALS" }
            }, statusCode: StatusCodes.Status401Unauthorized);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt, _) = jwtTokenService.GenerateAccessToken(user, roles);

        var (rawRefreshToken, refreshEntity) = await refreshTokenService.CreateRefreshTokenAsync(
            user.Id,
            familyId: null,
            ipAddress: ip,
            userAgent: userAgent);

        // Append httpOnly cookie for refresh token
        context.Response.Cookies.Append("refreshToken", rawRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Set to true in prod over HTTPS
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = refreshEntity.ExpiresAt
        });

        await auditService.LogAsync(new AuditLogEntry(
            Action: "AUTH_LOGIN_SUCCESS",
            EntityType: "User",
            EntityId: user.Id.ToString(),
            ActorUserId: user.Id,
            ActorRole: roles.FirstOrDefault() ?? "Customer",
            ActorIp: ip,
            UserAgent: userAgent,
            Outcome: AuditOutcome.Success
        ));

        return Results.Ok(new
        {
            accessToken,
            refreshToken = rawRefreshToken,
            tokenType = "Bearer",
            expiresAt,
            user = new
            {
                id = user.Id,
                email = user.Email,
                customerId = user.CustomerId,
                roles
            }
        });
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshRequest? request,
        [FromServices] IRefreshTokenService refreshTokenService,
        HttpContext context)
    {
        var rawToken = request?.RefreshToken
                       ?? context.Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Missing Token",
                Detail = "Refresh token is required.",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errorCode"] = "REFRESH_TOKEN_REQUIRED" }
            });
        }

        var ip = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();

        var result = await refreshTokenService.RotateRefreshTokenAsync(rawToken, ip, userAgent);

        if (!result.Success)
        {
            // Clear invalid cookie
            context.Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/v1/auth" });

            return Results.Json(new ProblemDetails
            {
                Title = "Token Refresh Failed",
                Detail = result.Error ?? "Failed to refresh token.",
                Status = StatusCodes.Status401Unauthorized,
                Extensions =
                {
                    ["errorCode"] = result.ReuseDetected ? "TOKEN_REUSE_DETECTED" : "INVALID_REFRESH_TOKEN",
                    ["reuseDetected"] = result.ReuseDetected
                }
            }, statusCode: StatusCodes.Status401Unauthorized);
        }

        context.Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = result.ExpiresAt
        });

        return Results.Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            tokenType = "Bearer",
            expiresAt = result.ExpiresAt
        });
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] RefreshRequest? request,
        [FromServices] IRefreshTokenService refreshTokenService,
        HttpContext context)
    {
        var rawToken = request?.RefreshToken
                       ?? context.Request.Cookies["refreshToken"];

        var ip = context.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            await refreshTokenService.RevokeTokenAsync(rawToken, ip);
        }

        context.Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/v1/auth" });

        return Results.Ok(new { message = "Logged out successfully." });
    }
}
