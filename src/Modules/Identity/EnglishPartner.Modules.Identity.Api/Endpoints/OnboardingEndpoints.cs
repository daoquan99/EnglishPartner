using EnglishPartner.Modules.Identity.Application.Onboarding.Commands.CompleteOnboarding;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace EnglishPartner.Modules.Identity.Api.Endpoints;

internal static class OnboardingEndpoints
{
    public static void MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/onboarding").WithTags("Onboarding").RequireAuthorization();

        group.MapPost("/complete", async (CompleteOnboardingRequest req, ClaimsPrincipal user, ISender sender) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var result = await sender.Send(new CompleteOnboardingCommand(
                userId.Value, req.NativeLanguage, req.TargetLanguage,
                req.ProficiencyLevel, req.LearningGoal, req.DailyGoalMinutes));

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

internal sealed record CompleteOnboardingRequest(
    string NativeLanguage, string TargetLanguage, string ProficiencyLevel,
    string LearningGoal, int DailyGoalMinutes);
