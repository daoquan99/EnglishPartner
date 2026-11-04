using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Progress.Queries.GetProgressOverview;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class ProgressEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/progress").RequireAuthorization();

        group.MapGet("/overview", async (
            int? days,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            int effectiveDays = days ?? 30;
            var overview = await sender.Send(new GetProgressOverviewQuery(currentUser.UserId!, effectiveDays), ct);
            return Results.Ok(overview);
        });

        return app;
    }
}
