using EnglishPartner.Modules.Identity.Application.Streaks.Commands.CheckIn;
using EnglishPartner.Modules.Identity.Application.Streaks.Queries.GetStreak;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace EnglishPartner.Modules.Identity.Api.Endpoints;

internal static class StreakEndpoints
{
    public static void MapStreakEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/streaks").WithTags("Streaks").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, ISender sender) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var result = await sender.Send(new GetStreakQuery(userId.Value));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapPost("/check-in", async (ClaimsPrincipal user, ISender sender) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var result = await sender.Send(new CheckInCommand(userId.Value));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
