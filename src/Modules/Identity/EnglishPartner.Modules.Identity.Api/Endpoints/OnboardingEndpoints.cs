using EnglishPartner.Modules.Identity.Application.Onboarding.Commands.CompleteOnboarding;
using EnglishPartner.SharedKernel.Extensions;
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
        var group = app.MapGroup("/api/v1/onboarding")
            .WithTags("Onboarding")
            .RequireAuthorization();

        group.MapPost("/complete", async (CompleteOnboardingRequest req, ClaimsPrincipal user, ISender sender) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();

            var result = await sender.Send(new CompleteOnboardingCommand(
                userId, req.NativeLanguage, req.TargetLanguage,
                req.ProficiencyLevel, req.LearningGoal, req.DailyGoalMinutes));

            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("CompleteOnboarding")
        .WithSummary("Complete the onboarding flow. Sets native/target language, level, goal, and daily target.");
    }
}

internal sealed record CompleteOnboardingRequest(
    string NativeLanguage, string TargetLanguage, string ProficiencyLevel,
    string LearningGoal, int DailyGoalMinutes);
