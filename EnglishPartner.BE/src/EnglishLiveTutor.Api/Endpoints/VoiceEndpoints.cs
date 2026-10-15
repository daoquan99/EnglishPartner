using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Voices.Commands.UpsertTeacherVoice;
using EnglishLiveTutor.Application.Features.Voices.Queries.GetTeacherVoiceById;
using EnglishLiveTutor.Application.Features.Voices.Queries.ListTeacherVoices;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Api.Endpoints;

public static class VoiceEndpoints
{
    public static IEndpointRouteBuilder MapVoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/voices");

        group.MapGet("/", async (
            HttpContext httpContext,
            bool? onlyEnabled,
            bool? onlyFeatured,
            string? gender,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            IQueryHandler<ListTeacherVoicesQuery, PagedResultDto<TeacherVoiceDto>> handler,
            CancellationToken ct) =>
        {
            var pagedVoices = await handler.HandleAsync(
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
            IQueryHandler<GetTeacherVoiceByIdQuery, TeacherVoiceDto?> handler,
            CancellationToken ct) =>
        {
            var voice = await handler.HandleAsync(new GetTeacherVoiceByIdQuery(id), ct);
            return voice is null ? Results.NotFound() : Results.Ok(voice);
        });

        group.MapPost("/", async (
            UpsertTeacherVoiceRequest req,
            ICommandHandler<UpsertTeacherVoiceCommand, TeacherVoiceDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                string id = string.IsNullOrWhiteSpace(req.Name) ? $"Voice_{Guid.NewGuid().ToString("N")[..6]}" : req.Name.Trim();
                var created = await handler.HandleAsync(new UpsertTeacherVoiceCommand(id, req), ct);
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

        group.MapDelete("/{id}", async (
            string id,
            ICurrentUser currentUser,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var voice = await db.TeacherVoices.FirstOrDefaultAsync(v => v.Id == id, ct);
            if (voice is null) return Results.NotFound();
            voice.SoftDelete(DateTimeOffset.UtcNow, currentUser.UserIdOrSystem);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequirePermission("voices.manage");

        return app;
    }
}
