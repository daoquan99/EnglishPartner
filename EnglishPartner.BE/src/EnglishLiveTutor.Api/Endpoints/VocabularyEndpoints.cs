using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyGenerationModels;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Vocabulary;
using Microsoft.EntityFrameworkCore;

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

        // List all Vocabulary Flashcards for Admin CRUD & Student Search (Database-level Pagination without arbitrary 200 cap)
        group.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            string? cefrLevel,
            string? topicCategory,
            int? page,
            int? pageSize,
            bool? paged,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var joinedQuery =
                from s in db.VocabularySenses.AsNoTracking()
                join v in db.Vocabularies.AsNoTracking() on s.VocabularyId equals v.Id
                select new { Vocab = v, Sense = s };

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLowerInvariant();
                joinedQuery = joinedQuery.Where(x =>
                    x.Vocab.NormalizedWord.Contains(term) ||
                    (x.Vocab.TopicCategory != null && x.Vocab.TopicCategory.ToLower().Contains(term)) ||
                    x.Sense.MeaningVi.ToLower().Contains(term) ||
                    x.Sense.DefinitionEn.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(topicCategory))
            {
                string top = topicCategory.Trim();
                joinedQuery = joinedQuery.Where(x => x.Vocab.TopicCategory == top);
            }

            if (!string.IsNullOrWhiteSpace(cefrLevel))
            {
                string normCefr = cefrLevel.Trim().ToUpperInvariant();
                joinedQuery = joinedQuery.Where(x => x.Sense.CefrLevel == normCefr);
            }

            int totalCount = await joinedQuery.CountAsync(ct);

            var orderedPairs = joinedQuery
                .OrderByDescending(x => x.Vocab.CreatedAtUtc)
                .ThenBy(x => x.Sense.DisplayOrder)
                .Select(x => new { VocabularyId = x.Vocab.Id, SenseId = x.Sense.Id });

            bool hasPaging = page.HasValue || pageSize.HasValue;
            int effPage = Math.Max(1, page ?? 1);
            int effSize = hasPaging
                ? Math.Clamp(pageSize ?? 20, 1, 500)
                : Math.Max(1, totalCount);

            var pagePairs = hasPaging
                ? await orderedPairs.Skip((effPage - 1) * effSize).Take(effSize).ToListAsync(ct)
                : await orderedPairs.ToListAsync(ct);

            var pageVocabIds = pagePairs.Select(p => p.VocabularyId).Distinct().ToList();
            var vocabDict = await db.Vocabularies
                .AsNoTracking()
                .Include(v => v.Pronunciations)
                .Include(v => v.WordFamilies)
                .Include(v => v.Senses).ThenInclude(s => s.Relations)
                .Include(v => v.Senses).ThenInclude(s => s.Collocations)
                .Include(v => v.Senses).ThenInclude(s => s.Examples)
                .Include(v => v.Senses).ThenInclude(s => s.GrammarPatterns)
                .Where(v => pageVocabIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, ct);

            var cards = new List<VocabularyFlashcardDto>(pagePairs.Count);
            foreach (var pair in pagePairs)
            {
                if (!vocabDict.TryGetValue(pair.VocabularyId, out var vocab))
                    continue;
                var sense = vocab.Senses.FirstOrDefault(s => s.Id == pair.SenseId);
                if (sense is null)
                    continue;

                cards.Add(VocabularyProjectionHelper.MapToFlashcardDto(
                    Guid.Empty,
                    vocab,
                    sense,
                    "New",
                    "Pending",
                    null,
                    null));
            }

            var pagedResult = PagedResultDto<VocabularyFlashcardDto>.FromPage(cards, totalCount, page, pageSize);
            return pagedResult.ToHttpResult(httpContext, paged);
        });

        group.MapGet("/{vocabularyId:guid}/flashcard", async (
            Guid vocabularyId,
            Guid? senseId,
            string? userId,
            IQueryHandler<GetVocabularyFlashcardQuery, VocabularyFlashcardDto?> handler,
            CancellationToken ct) =>
        {
            var dto = await handler.HandleAsync(
                new GetVocabularyFlashcardQuery(vocabularyId, senseId, userId ?? "default-learner"),
                ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapPost("/generate", async (
            GenerateVocabularyRequest req,
            ICurrentUser currentUser,
            ICommandHandler<GenerateVocabularyCommand, List<VocabularyFlashcardDto>> handler,
            CancellationToken ct) =>
        {
            try
            {
                string userId = string.IsNullOrWhiteSpace(currentUser.UserId) ? "default-learner" : currentUser.UserId!;
                var cards = await handler.HandleAsync(new GenerateVocabularyCommand(userId, req), ct);
                return Results.Ok(cards);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Admin Create Vocabulary Flashcard
        group.MapPost("/", async (
            UpsertVocabularyAdminRequest req,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Word))
                return Results.BadRequest(new { error = "Từ vựng (Word) không được để trống." });

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var vocab = VocabularyEntity.Create(
                word: req.Word.Trim(),
                languageCode: "en",
                sourceType: "AdminCreated",
                frequencyRank: req.FrequencyRank,
                topicCategory: req.TopicCategory);

            if (!string.IsNullOrWhiteSpace(req.UkIpa))
                vocab.AddPronunciation("UK", req.UkIpa.Trim(), stressPattern: req.StressPattern);
            if (!string.IsNullOrWhiteSpace(req.UsIpa))
                vocab.AddPronunciation("US", req.UsIpa.Trim(), stressPattern: req.StressPattern);

            if (req.WordFamilies is not null)
            {
                foreach (var wf in req.WordFamilies.Where(x => !string.IsNullOrWhiteSpace(x.WordForm)))
                    vocab.AddWordFamily(wf.WordForm.Trim(), wf.PartOfSpeech);
            }

            var sense = vocab.AddSense(
                partOfSpeech: string.IsNullOrWhiteSpace(req.PartOfSpeech) ? "noun" : req.PartOfSpeech.Trim(),
                definitionEn: string.IsNullOrWhiteSpace(req.DefinitionEn) ? req.Word.Trim() : req.DefinitionEn.Trim(),
                meaningVi: string.IsNullOrWhiteSpace(req.MeaningVi) ? req.Word.Trim() : req.MeaningVi.Trim(),
                cefrLevel: string.IsNullOrWhiteSpace(req.CefrLevel) ? "B1" : req.CefrLevel.Trim().ToUpperInvariant(),
                descriptionEn: req.DescriptionEn ?? string.Empty,
                isPrimary: true,
                displayOrder: 1);

            if (req.Synonyms is not null)
            {
                int order = 1;
                foreach (var syn in req.Synonyms.Where(x => !string.IsNullOrWhiteSpace(x)))
                    sense.AddRelation("Synonym", syn.Trim(), order++);
            }

            if (req.Antonyms is not null)
            {
                int order = 1;
                foreach (var ant in req.Antonyms.Where(x => !string.IsNullOrWhiteSpace(x)))
                    sense.AddRelation("Antonym", ant.Trim(), order++);
            }

            if (req.Collocations is not null)
            {
                int order = 1;
                foreach (var col in req.Collocations.Where(x => !string.IsNullOrWhiteSpace(x.Phrase)))
                    sense.AddCollocation(col.Phrase.Trim(), col.MeaningVi, order++);
            }

            if (req.Examples is not null)
            {
                int order = 1;
                foreach (var ex in req.Examples.Where(x => !string.IsNullOrWhiteSpace(x.SentenceEn)))
                    sense.AddExample(ex.SentenceEn.Trim(), ex.TranslationVi, ex.CefrLevel ?? req.CefrLevel, order++);
            }

            if (req.GrammarPatterns is not null)
            {
                foreach (var gp in req.GrammarPatterns.Where(x => !string.IsNullOrWhiteSpace(x.Pattern)))
                    sense.AddGrammarPattern(gp.Pattern.Trim(), gp.ExplanationVi, gp.SampleSentence);
            }

            db.Vocabularies.Add(vocab);
            await db.SaveChangesAsync(ct);

            return Results.Ok(VocabularyProjectionHelper.MapToFlashcardDto(
                Guid.Empty,
                vocab,
                sense,
                "New",
                "Pending",
                null,
                null));
        }).RequirePermission("vocabularies.manage");

        // Admin Update Vocabulary Flashcard
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpsertVocabularyAdminRequest req,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var existing = await db.Vocabularies
                .Include(v => v.Pronunciations)
                .Include(v => v.WordFamilies)
                .Include(v => v.Senses).ThenInclude(s => s.Relations)
                .Include(v => v.Senses).ThenInclude(s => s.Collocations)
                .Include(v => v.Senses).ThenInclude(s => s.Examples)
                .Include(v => v.Senses).ThenInclude(s => s.GrammarPatterns)
                .FirstOrDefaultAsync(v => v.Id == id, ct);

            if (existing is null)
                return Results.NotFound(new { error = "Không tìm thấy từ vựng cần cập nhật." });

            db.Vocabularies.Remove(existing);
            await db.SaveChangesAsync(ct);

            var updated = VocabularyEntity.Create(
                word: req.Word.Trim(),
                languageCode: "en",
                sourceType: "AdminUpdated",
                frequencyRank: req.FrequencyRank,
                topicCategory: req.TopicCategory,
                id: id);

            if (!string.IsNullOrWhiteSpace(req.UkIpa))
                updated.AddPronunciation("UK", req.UkIpa.Trim(), stressPattern: req.StressPattern);
            if (!string.IsNullOrWhiteSpace(req.UsIpa))
                updated.AddPronunciation("US", req.UsIpa.Trim(), stressPattern: req.StressPattern);

            if (req.WordFamilies is not null)
            {
                foreach (var wf in req.WordFamilies.Where(x => !string.IsNullOrWhiteSpace(x.WordForm)))
                    updated.AddWordFamily(wf.WordForm.Trim(), wf.PartOfSpeech);
            }

            var sense = updated.AddSense(
                partOfSpeech: string.IsNullOrWhiteSpace(req.PartOfSpeech) ? "noun" : req.PartOfSpeech.Trim(),
                definitionEn: string.IsNullOrWhiteSpace(req.DefinitionEn) ? req.Word.Trim() : req.DefinitionEn.Trim(),
                meaningVi: string.IsNullOrWhiteSpace(req.MeaningVi) ? req.Word.Trim() : req.MeaningVi.Trim(),
                cefrLevel: string.IsNullOrWhiteSpace(req.CefrLevel) ? "B1" : req.CefrLevel.Trim().ToUpperInvariant(),
                descriptionEn: req.DescriptionEn ?? string.Empty,
                isPrimary: true,
                displayOrder: 1);

            if (req.Synonyms is not null)
            {
                int order = 1;
                foreach (var syn in req.Synonyms.Where(x => !string.IsNullOrWhiteSpace(x)))
                    sense.AddRelation("Synonym", syn.Trim(), order++);
            }

            if (req.Antonyms is not null)
            {
                int order = 1;
                foreach (var ant in req.Antonyms.Where(x => !string.IsNullOrWhiteSpace(x)))
                    sense.AddRelation("Antonym", ant.Trim(), order++);
            }

            if (req.Collocations is not null)
            {
                int order = 1;
                foreach (var col in req.Collocations.Where(x => !string.IsNullOrWhiteSpace(x.Phrase)))
                    sense.AddCollocation(col.Phrase.Trim(), col.MeaningVi, order++);
            }

            if (req.Examples is not null)
            {
                int order = 1;
                foreach (var ex in req.Examples.Where(x => !string.IsNullOrWhiteSpace(x.SentenceEn)))
                    sense.AddExample(ex.SentenceEn.Trim(), ex.TranslationVi, ex.CefrLevel ?? req.CefrLevel, order++);
            }

            if (req.GrammarPatterns is not null)
            {
                foreach (var gp in req.GrammarPatterns.Where(x => !string.IsNullOrWhiteSpace(x.Pattern)))
                    sense.AddGrammarPattern(gp.Pattern.Trim(), gp.ExplanationVi, gp.SampleSentence);
            }

            db.Vocabularies.Add(updated);
            await db.SaveChangesAsync(ct);

            return Results.Ok(VocabularyProjectionHelper.MapToFlashcardDto(
                Guid.Empty,
                updated,
                sense,
                "New",
                "Pending",
                null,
                null));
        }).RequirePermission("vocabularies.manage");

        // Admin Delete Vocabulary Flashcard
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var vocab = await db.Vocabularies.FirstOrDefaultAsync(v => v.Id == id, ct);
            if (vocab is null) return Results.NotFound();
            vocab.SoftDelete(DateTimeOffset.UtcNow, currentUser.UserIdOrSystem);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequirePermission("vocabularies.manage");

        return app;
    }
}
