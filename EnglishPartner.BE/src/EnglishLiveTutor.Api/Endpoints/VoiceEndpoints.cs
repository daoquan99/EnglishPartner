using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Voices.Commands.UpsertTeacherVoice;
using EnglishLiveTutor.Application.Features.Voices.Queries.GetTeacherVoiceById;
using EnglishLiveTutor.Application.Features.Voices.Queries.ListTeacherVoices;

namespace EnglishLiveTutor.Api.Endpoints;

public static class VoiceEndpoints
{
    public static IEndpointRouteBuilder MapVoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/voices");

        group.MapGet("/", async (
            bool? onlyEnabled,
            bool? onlyFeatured,
            string? gender,
            IQueryHandler<ListTeacherVoicesQuery, List<TeacherVoiceDto>> handler,
            CancellationToken ct) =>
        {
            var voices = await handler.HandleAsync(
                new ListTeacherVoicesQuery(
                    OnlyEnabled: onlyEnabled ?? true,
                    OnlyFeatured: onlyFeatured ?? false,
                    Gender: gender),
                ct);
            return Results.Ok(voices);
        });

        group.MapGet("/{id}", async (
            string id,
            IQueryHandler<GetTeacherVoiceByIdQuery, TeacherVoiceDto?> handler,
            CancellationToken ct) =>
        {
            var voice = await handler.HandleAsync(new GetTeacherVoiceByIdQuery(id), ct);
            return voice is null ? Results.NotFound() : Results.Ok(voice);
        });

        group.MapPut("/{id}", async (
            string id,
            UpsertTeacherVoiceRequest req,
            ICommandHandler<UpsertTeacherVoiceCommand, TeacherVoiceDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var updated = await handler.HandleAsync(new UpsertTeacherVoiceCommand(id, req), ct);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("voices.manage");

        return app;
    }
}
