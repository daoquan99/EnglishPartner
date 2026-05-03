using EnglishPartner.Modules.Identity.Application.Streaks.Commands.CheckIn;
using EnglishPartner.Modules.Identity.Application.Streaks.Queries.GetStreak;
using EnglishPartner.SharedKernel.Extensions;
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
        var group = app.MapGroup("/api/v1/streaks")
            .WithTags("Streaks")
            .RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, ISender sender) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetStreakQuery(userId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetStreak")
        .WithSummary("Get the current user's streak, including current count, longest, and freeze tokens.");

        group.MapPost("/check-in", async (ClaimsPrincipal user, ISender sender) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new CheckInCommand(userId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("CheckIn")
        .WithSummary("Record today's check-in to maintain or extend the learning streak.");
    }
}
