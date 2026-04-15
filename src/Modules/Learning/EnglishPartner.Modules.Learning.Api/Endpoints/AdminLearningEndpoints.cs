using EnglishPartner.Modules.Learning.Application.Admin.Commands.AdjustUserXp;
using EnglishPartner.Modules.Learning.Application.Admin.Commands.DeleteItemMastery;
using EnglishPartner.Modules.Learning.Application.Admin.Commands.ForceAbandonSession;
using EnglishPartner.Modules.Learning.Application.Admin.Commands.ResetUserMasteries;
using EnglishPartner.Modules.Learning.Application.Admin.Commands.ResetUserXp;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Learning.Api.Endpoints;

/// <summary>
/// Admin endpoints for managing user learning data.
/// All require Admin role + learning.admin permission.
/// </summary>
public static class AdminLearningEndpoints
{
    public static void MapAdminLearningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/learning")
            .RequireAuthorization("learning.admin.write")
            .WithTags("Admin — Learning");

        // PUT /api/v1/admin/learning/users/{userId}/xp
        group.MapPut("/users/{userId:guid}/xp", async (
            Guid userId,
            AdjustXpRequest body,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new AdjustUserXpCommand(userId, body.Amount, body.Reason), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        // POST /api/v1/admin/learning/users/{userId}/xp/reset
        group.MapPost("/users/{userId:guid}/xp/reset", async (
            Guid userId, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ResetUserXpCommand(userId), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        // DELETE /api/v1/admin/learning/users/{userId}/masteries/{itemId}
        group.MapDelete("/users/{userId:guid}/masteries/{itemId:guid}", async (
            Guid userId, Guid itemId, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteItemMasteryCommand(userId, itemId), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        // POST /api/v1/admin/learning/users/{userId}/masteries/reset
        group.MapPost("/users/{userId:guid}/masteries/reset", async (
            Guid userId, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ResetUserMasteriesCommand(userId), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        // POST /api/v1/admin/learning/sessions/{id}/abandon  — force abandon an in-progress session
        group.MapPost("/sessions/{id:guid}/abandon", async (
            Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ForceAbandonSessionCommand(id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });
    }

    private sealed record AdjustXpRequest(int Amount, string Reason);
}
