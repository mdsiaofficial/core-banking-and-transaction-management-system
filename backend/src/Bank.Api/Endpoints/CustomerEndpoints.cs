using System.Security.Claims;
using Bank.Api.Security;
using Bank.Application.Customers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Bank.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/me")
            .WithTags("Current Customer Profile")
            .RequireAuthorization();

        group.MapGet("/", GetMyProfileAsync)
            .WithName("GetMyProfile")
            .WithSummary("Get profile of authenticated customer");

        group.MapPatch("/", UpdateMyProfileAsync)
            .WithName("UpdateMyProfile")
            .WithSummary("Update profile details of authenticated customer");

        return routes;
    }

    private static async Task<IResult> GetMyProfileAsync(
        [FromServices] ICustomerService customerService,
        [FromServices] ApplicationDbContextLocator locator,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var customerId = await locator.ResolveCustomerIdAsync(user, ct);
        if (!customerId.HasValue) return Results.NotFound();

        var customer = await customerService.GetCustomerByIdAsync(customerId.Value, ct);
        return customer is null ? Results.NotFound() : Results.Ok(customer);
    }

    private static async Task<IResult> UpdateMyProfileAsync(
        [FromBody] UpdateProfileRequest request,
        [FromServices] ICustomerService customerService,
        [FromServices] ApplicationDbContextLocator locator,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var customerId = await locator.ResolveCustomerIdAsync(user, ct);
        if (!customerId.HasValue) return Results.NotFound();

        await customerService.UpdateProfileAsync(customerId.Value, request, ct);
        var updated = await customerService.GetCustomerByIdAsync(customerId.Value, ct);
        return Results.Ok(updated);
    }
}
