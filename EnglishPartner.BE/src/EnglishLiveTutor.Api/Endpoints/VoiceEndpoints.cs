using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Voices.Commands.DeleteTeacherVoice;
using EnglishLiveTutor.Application.Features.Voices.Commands.UpsertTeacherVoice;
using EnglishLiveTutor.Application.Features.Voices.Queries.GetTeacherVoiceById;
using EnglishLiveTutor.Application.Features.Voices.Queries.ListTeacherVoices;
using MediatR;

namespace EnglishLiveTutor.Api.Endpoints;

public static class VoiceEndpoints
{
    public static IEndpointRouteBuilder MapVoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/voices").RequireAuthorization();

        group.MapGet("/", async (
            HttpContext httpContext,
            bool? onlyEnabled,
            bool? onlyFeatured,
            string? gender,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            var pagedVoices = await sender.Send(
                new ListTeacherVoicesQuery(
                    OnlyEnabled: onlyEnabled ?? true,
                    OnlyFeatured: onlyFeatured ?? false,
                    Gender: gender,
                    Search: search,
                    Page: page,
                    PageSize: pageSize),
                ct);
            return pagedVoices.ToHttpResult(httpContext, paged);
        });

        group.MapGet("/{id}", async (
            string id,
            ISender sender,
            CancellationToken ct) =>
        {
            var voice = await sender.Send(new GetTeacherVoiceByIdQuery(id), ct);
            return voice is null ? Results.NotFound() : Results.Ok(voice);
        });

        group.MapPost("/", async (
            UpsertTeacherVoiceRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                string id = string.IsNullOrWhiteSpace(req.Name) ? $"Voice_{Guid.NewGuid().ToString("N")[..6]}" : req.Name.Trim();
                var created = await sender.Send(new UpsertTeacherVoiceCommand(id, req), ct);
                return Results.Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("voices.manage");

        group.MapPut("/{id}", async (
            string id,
            UpsertTeacherVoiceRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var updated = await sender.Send(new UpsertTeacherVoiceCommand(id, req), ct);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("voices.manage");

        group.MapDelete("/{id}", async (
            string id,
            ISender sender,
            CancellationToken ct) =>
        {
            bool deleted = await sender.Send(new DeleteTeacherVoiceCommand(id), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequirePermission("voices.manage");

        return app;
    }
}
