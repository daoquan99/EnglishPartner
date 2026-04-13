using EnglishPartner.Modules.Identity.Application.Profile.Commands.UpdateMyProfile;
using EnglishPartner.Modules.Identity.Application.Profile.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace EnglishPartner.Modules.Identity.Api.Endpoints;

internal static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile").WithTags("Profile").RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal user, ISender sender) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var result = await sender.Send(new GetMyProfileQuery(userId.Value));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapPut("/me", async (UpdateProfileRequest req, ClaimsPrincipal user, ISender sender) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var result = await sender.Send(new UpdateMyProfileCommand(
                userId.Value, req.DisplayName, req.NativeLanguage, req.TargetLanguage,
                req.ProficiencyLevel, req.LearningGoal, req.DailyGoalMinutes,
                req.AvatarUrl, req.DateOfBirth));

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

internal sealed record UpdateProfileRequest(
    string? DisplayName, string? NativeLanguage, string? TargetLanguage,
    string? ProficiencyLevel, string? LearningGoal, int? DailyGoalMinutes,
    string? AvatarUrl, DateTime? DateOfBirth);
