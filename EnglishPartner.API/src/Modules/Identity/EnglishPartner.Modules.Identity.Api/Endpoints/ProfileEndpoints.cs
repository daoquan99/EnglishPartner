using EnglishPartner.Modules.Identity.Application.Profile.Commands.UpdateMyProfile;
using EnglishPartner.Modules.Identity.Application.Profile.Queries.GetMyProfile;
using EnglishPartner.SharedKernel.Extensions;
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
        var group = app.MapGroup("/api/v1/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal user, ISender sender) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetMyProfileQuery(userId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetMyProfile")
        .WithSummary("Get the current user's profile, including learning preferences and streak.");

        group.MapPut("/me", async (UpdateProfileRequest req, ClaimsPrincipal user, ISender sender) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();

            var result = await sender.Send(new UpdateMyProfileCommand(
                userId, req.DisplayName, req.NativeLanguage, req.TargetLanguage,
                req.ProficiencyLevel, req.LearningGoal, req.DailyGoalMinutes,
                req.AvatarUrl, req.DateOfBirth,
                req.MorningReminderHour, req.EveningReminderHour, req.UtcOffsetHours));

            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("UpdateMyProfile")
        .WithSummary("Update the current user's profile, preferences, avatar, and study reminders.");
    }
}

internal sealed record UpdateProfileRequest(
    string? DisplayName, string? NativeLanguage, string? TargetLanguage,
    string? ProficiencyLevel, string? LearningGoal, int? DailyGoalMinutes,
    string? AvatarUrl, DateTime? DateOfBirth,
    int? MorningReminderHour, int? EveningReminderHour, int? UtcOffsetHours);
