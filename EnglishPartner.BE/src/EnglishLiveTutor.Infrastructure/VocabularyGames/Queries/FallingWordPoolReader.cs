using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Exceptions;
using EnglishLiveTutor.Application.Features.VocabularyGames.Ports;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.VocabularyGames.Queries;

public sealed class FallingWordPoolReader : IFallingWordPoolReader
{
    private readonly AppDbContext _db;

    public FallingWordPoolReader(AppDbContext db)
    {
        _db = db;
    }

    public async Task<FallingWordPoolCountDto> GetPoolCountAsync(
        string userId,
        FallingWordGameSource source,
        string? cefrLevel,
        FallingWordPlayFilter playFilter,
        string? timeZoneId,
        CancellationToken ct = default)
    {
        var (allCandidates, _) = await LoadCandidatesAsync(userId, source, cefrLevel, timeZoneId, ct);

        // Group by NormalizedWord to enforce 1 sense per distinct word
        var distinctWords = allCandidates
            .GroupBy(c => c.NormalizedWord)
            .Select(g => g.First())
            .ToList();

        int allCount = distinctWords.Count;
        int unplayedCount = distinctWords.Count(c => c.HistoryOutcome == FallingWordPlayFilter.Unplayed);
        int correctCount = distinctWords.Count(c => c.HistoryOutcome == FallingWordPlayFilter.Correct);
        int failedCount = distinctWords.Count(c => c.HistoryOutcome == FallingWordPlayFilter.Failed);
        int playedCount = correctCount + failedCount;

        int totalValidCount = playFilter switch
        {
            FallingWordPlayFilter.Unplayed => unplayedCount,
            FallingWordPlayFilter.Played => playedCount,
            FallingWordPlayFilter.Correct => correctCount,
            FallingWordPlayFilter.Failed => failedCount,
            _ => allCount
        };

        return new FallingWordPoolCountDto(
            TotalValidCount: totalValidCount,
            Source: source.ToString(),
            CefrLevel: cefrLevel,
            PlayFilter: playFilter.ToString(),
            AllCount: allCount,
            UnplayedCount: unplayedCount,
            PlayedCount: playedCount,
            CorrectCount: correctCount,
            FailedCount: failedCount);
    }

    public async Task<(List<CandidatePoolItem> Selected, int TotalPoolCount, Guid? DailyPlanId, string? LearningDate)> SelectPoolForSessionAsync(
        string userId,
        FallingWordGameSource source,
        string? cefrLevel,
        FallingWordPlayFilter playFilter,
        int requestedCount,
        string? timeZoneId,
        CancellationToken ct = default)
    {
        var (allCandidates, planInfo) = await LoadCandidatesAsync(userId, source, cefrLevel, timeZoneId, ct);

        // Filter candidates by playFilter
        var filtered = playFilter switch
        {
            FallingWordPlayFilter.Unplayed => allCandidates.Where(c => c.HistoryOutcome == FallingWordPlayFilter.Unplayed),
            FallingWordPlayFilter.Played => allCandidates.Where(c => c.HistoryOutcome == FallingWordPlayFilter.Correct || c.HistoryOutcome == FallingWordPlayFilter.Failed),
            FallingWordPlayFilter.Correct => allCandidates.Where(c => c.HistoryOutcome == FallingWordPlayFilter.Correct),
            FallingWordPlayFilter.Failed => allCandidates.Where(c => c.HistoryOutcome == FallingWordPlayFilter.Failed),
            _ => allCandidates
        };

        // Group by NormalizedWord so no word repeats in the session
        var candidateList = filtered
            .GroupBy(c => c.NormalizedWord)
            .Select(g => g.First())
            .ToList();

        int totalPoolCount = candidateList.Count;
        if (totalPoolCount == 0)
        {
            return (new List<CandidatePoolItem>(), 0, planInfo?.DailyPlanId, planInfo?.LearningDate);
        }

        // Shuffle candidates server-side
        var rng = Random.Shared;
        var shuffled = candidateList.OrderBy(_ => rng.Next()).ToList();

        int takeCount = Math.Min(requestedCount, totalPoolCount);
        var selected = shuffled.Take(takeCount).ToList();

        return (selected, totalPoolCount, planInfo?.DailyPlanId, planInfo?.LearningDate);
    }

    private sealed record PlanInfo(Guid DailyPlanId, string LearningDate);

    private async Task<(List<CandidatePoolItem> Candidates, PlanInfo? Plan)> LoadCandidatesAsync(
        string userId,
        FallingWordGameSource source,
        string? cefrLevel,
        string? timeZoneId,
        CancellationToken ct)
    {
        string? normalizedCefr = string.IsNullOrWhiteSpace(cefrLevel) ? null : cefrLevel.Trim().ToUpperInvariant();

        PlanInfo? planInfo = null;
        List<RawSenseCandidate> rawList;

        if (source == FallingWordGameSource.DailyVocabulary)
        {
            string learningDate = DailyVocabularyPlanEntity.ResolveLocalLearningDate(timeZoneId);
            var plan = await _db.DailyVocabularyPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LearningDate == learningDate, ct);

            if (plan == null)
            {
                throw new DailyPlanNotFoundException($"Chưa có kế hoạch Daily Vocabulary cho ngày {learningDate}. Vui lòng mở trang Daily Vocabulary để khởi tạo.");
            }

            planInfo = new PlanInfo(plan.Id, learningDate);

            var query = from item in _db.DailyVocabularyItems.AsNoTracking()
                        join v in _db.Vocabularies.AsNoTracking() on item.VocabularyId equals v.Id
                        join s in _db.VocabularySenses.AsNoTracking() on item.SenseId equals s.Id
                        where item.PlanId == plan.Id &&
                              !v.IsDeleted &&
                              v.IsActive &&
                              v.Word != null && v.Word != "" &&
                              s.MeaningVi != null && s.MeaningVi != ""
                        select new { item, v, s };

            if (!string.IsNullOrEmpty(normalizedCefr))
            {
                query = query.Where(x => x.s.CefrLevel == normalizedCefr);
            }

            var rows = await query
                .Select(x => new
                {
                    x.v.Id,
                    SenseId = x.s.Id,
                    DailyItemId = (Guid?)x.item.Id,
                    x.v.Word,
                    x.v.NormalizedWord,
                    x.s.MeaningVi,
                    x.s.CefrLevel
                })
                .ToListAsync(ct);

            rawList = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Word) && !string.IsNullOrWhiteSpace(x.MeaningVi))
                .Select(x => new RawSenseCandidate(
                    x.Id,
                    x.SenseId,
                    x.DailyItemId,
                    x.Word.Trim(),
                    string.IsNullOrWhiteSpace(x.NormalizedWord) ? x.Word.Trim().ToLowerInvariant() : x.NormalizedWord.Trim().ToLowerInvariant(),
                    x.MeaningVi.Trim(),
                    x.CefrLevel))
                .ToList();
        }
        else
        {
            // Catalog source
            var query = from v in _db.Vocabularies.AsNoTracking()
                        join s in _db.VocabularySenses.AsNoTracking() on v.Id equals s.VocabularyId
                        where !v.IsDeleted &&
                              v.IsActive &&
                              v.Word != null && v.Word != "" &&
                              s.MeaningVi != null && s.MeaningVi != ""
                        select new { v, s };

            if (!string.IsNullOrEmpty(normalizedCefr))
            {
                query = query.Where(x => x.s.CefrLevel == normalizedCefr);
            }

            var rows = await query
                .Select(x => new
                {
                    x.v.Id,
                    SenseId = x.s.Id,
                    DailyItemId = (Guid?)null,
                    x.v.Word,
                    x.v.NormalizedWord,
                    x.s.MeaningVi,
                    x.s.CefrLevel
                })
                .ToListAsync(ct);

            rawList = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Word) && !string.IsNullOrWhiteSpace(x.MeaningVi))
                .Select(x => new RawSenseCandidate(
                    x.Id,
                    x.SenseId,
                    x.DailyItemId,
                    x.Word.Trim(),
                    string.IsNullOrWhiteSpace(x.NormalizedWord) ? x.Word.Trim().ToLowerInvariant() : x.NormalizedWord.Trim().ToLowerInvariant(),
                    x.MeaningVi.Trim(),
                    x.CefrLevel))
                .ToList();
        }

        if (rawList.Count == 0)
        {
            return (new List<CandidatePoolItem>(), planInfo);
        }

        // Fetch latest evaluated outcome per SenseId for current user
        // Evaluated outcomes: Correct, Incorrect, TimedOut
        var evaluatedRounds = await (from r in _db.FallingWordGameRounds.AsNoTracking()
                                     join s in _db.FallingWordGameSessions.AsNoTracking() on r.SessionId equals s.Id
                                     where s.UserId == userId &&
                                           (r.Status == FallingWordRoundStatus.Correct ||
                                            r.Status == FallingWordRoundStatus.Incorrect ||
                                            r.Status == FallingWordRoundStatus.TimedOut)
                                     orderby (r.StartedAtUtc ?? r.CreatedAtUtc) descending, r.Id descending
                                     select new { r.SenseId, r.Status })
                                     .ToListAsync(ct);

        var latestOutcomeBySense = evaluatedRounds
            .DistinctBy(r => r.SenseId)
            .ToDictionary(r => r.SenseId, r => r.Status);

        var candidates = rawList.Select(raw =>
        {
            FallingWordPlayFilter outcome = FallingWordPlayFilter.Unplayed;
            if (latestOutcomeBySense.TryGetValue(raw.SenseId, out var status))
            {
                outcome = status == FallingWordRoundStatus.Correct
                    ? FallingWordPlayFilter.Correct
                    : FallingWordPlayFilter.Failed;
            }

            return new CandidatePoolItem(
                VocabularyId: raw.VocabularyId,
                SenseId: raw.SenseId,
                DailyItemId: raw.DailyItemId,
                Word: raw.Word,
                NormalizedWord: raw.NormalizedWord,
                MeaningVi: raw.MeaningVi,
                CefrLevel: raw.CefrLevel,
                HistoryOutcome: outcome);
        }).ToList();

        return (candidates, planInfo);
    }

    private sealed record RawSenseCandidate(
        Guid VocabularyId,
        Guid SenseId,
        Guid? DailyItemId,
        string Word,
        string NormalizedWord,
        string MeaningVi,
        string? CefrLevel);
}
