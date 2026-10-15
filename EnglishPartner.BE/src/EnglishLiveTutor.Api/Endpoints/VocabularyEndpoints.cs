using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyGenerationModels;
using EnglishLiveTutor.Domain.AiCatalog;

namespace EnglishLiveTutor.Api.Endpoints;

public static class VocabularyEndpoints
{
    public static IEndpointRouteBuilder MapVocabularyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vocabularies");

        group.MapGet("/models/generation", async (
            IQueryHandler<GetVocabularyGenerationModelsQuery, List<AiModelConfigEntity>> handler,
            CancellationToken ct) =>
        {
            var models = await handler.HandleAsync(new GetVocabularyGenerationModelsQuery(), ct);
            return Results.Ok(models);
        });

        group.MapGet("/{id:guid}/flashcard", async (
            Guid id,
            Guid? senseId,
            IQueryHandler<GetVocabularyFlashcardQuery, VocabularyFlashcardDto?> handler,
            CancellationToken ct) =>
        {
            var card = await handler.HandleAsync(new GetVocabularyFlashcardQuery(id, senseId, "default-learner"), ct);
            return card is null ? Results.NotFound() : Results.Ok(card);
        });

        group.MapPost("/generate", async (
            GenerateVocabularyRequest req,
            ICommandHandler<GenerateVocabularyCommand, List<VocabularyFlashcardDto>> handler,
            CancellationToken ct) =>
        {
            try
            {
                var generated = await handler.HandleAsync(new GenerateVocabularyCommand("default-learner", req), ct);
                return Results.Ok(generated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return app;
    }
}
