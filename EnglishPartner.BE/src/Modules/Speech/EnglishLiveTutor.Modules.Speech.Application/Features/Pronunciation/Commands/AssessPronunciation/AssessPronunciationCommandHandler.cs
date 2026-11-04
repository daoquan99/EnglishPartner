using System.Text.Json;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.AiModels.Contracts;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.AssessPronunciation;

public sealed class AssessPronunciationCommandHandler : ICommandHandler<AssessPronunciationCommand, PronunciationAssessmentResultDto>
{
    private readonly ISpeechDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IPronunciationAiAssessor _aiAssessor;
    private readonly IAiModelCatalogReader _modelCatalogReader;
    private readonly IPronunciationTargetTextSource _targetTextSource;
    private readonly IPronunciationAssessmentObserver _assessmentObserver;

    public AssessPronunciationCommandHandler(
        ISpeechDbContext db,
        IUnitOfWork uow,
        IPronunciationAiAssessor aiAssessor,
        IAiModelCatalogReader modelCatalogReader,
        IPronunciationTargetTextSource targetTextSource,
        IPronunciationAssessmentObserver assessmentObserver)
    {
        _db = db;
        _uow = uow;
        _aiAssessor = aiAssessor;
        _modelCatalogReader = modelCatalogReader;
        _targetTextSource = targetTextSource;
        _assessmentObserver = assessmentObserver;
    }

    public async Task<PronunciationAssessmentResultDto> HandleAsync(AssessPronunciationCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var userId = command.UserId;

        if (string.IsNullOrWhiteSpace(req.ApiKey))
            throw new InvalidOperationException("Cần cung cấp API Key của Google Gemini để chấm phát âm Audio.");

        if (string.IsNullOrWhiteSpace(req.Base64Audio))
            throw new InvalidOperationException("Bản ghi âm không được để trống.");

        byte[] audioBytes;
        try
        {
            audioBytes = Convert.FromBase64String(req.Base64Audio);
        }
        catch
        {
            throw new InvalidOperationException("Định dạng âm thanh base64 không hợp lệ.");
        }

        if (req.DurationMilliseconds < 250 || req.DurationMilliseconds > 60000)
            throw new InvalidOperationException("Thời lượng bản ghi âm phải từ 0.25 giây đến 60 giây.");

        // 1. Validate selected model capabilities
        var modelMeta = await _modelCatalogReader.FindByModelCodeAsync(req.AssessmentModelCode, ct);

        if (modelMeta is null)
            throw new InvalidOperationException($"Model '{req.AssessmentModelCode}' không tồn tại trong hệ thống.");

        if (!modelMeta.SupportsAudioInput || !modelMeta.SupportsPronunciationAssessment)
        {
            throw new InvalidOperationException(
                $"Model '{modelMeta.DisplayName}' ({modelMeta.ModelCode}) KHÔNG hỗ trợ đầu vào âm thanh trực tiếp để chấm phát âm. " +
                "Vui lòng chọn các dòng hỗ trợ Audio như Gemini 3.8 Flash, Gemini 3.6 Flash, hoặc Gemini 3.5 Flash Lite.");
        }

        string effectiveModelCode = modelMeta.EffectiveModelCode;

        // 2. Resolve authoritative expected reference text server-side
        var vocab = await _targetTextSource.GetAssessmentVocabularyAsync(req.VocabularyId, ct);

        if (vocab is null)
            throw new InvalidOperationException("Không tìm thấy từ vựng cần chấm phát âm.");

        string targetType = string.Equals(req.TargetType, "Sentence", StringComparison.OrdinalIgnoreCase) ? "Sentence" : "Word";
        string expectedText = vocab.Word;

        if (targetType == "Sentence")
        {
            var matchedEx = req.TargetId.HasValue
                ? vocab.Examples.FirstOrDefault(e => e.Id == req.TargetId.Value)
                : vocab.Examples.FirstOrDefault();

            if (matchedEx is not null)
            {
                expectedText = matchedEx.SentenceEn;
            }
            else if (!string.IsNullOrWhiteSpace(req.ReferenceText))
            {
                expectedText = req.ReferenceText.Trim();
            }
        }

        string accent = string.Equals(req.Accent, "UK", StringComparison.OrdinalIgnoreCase) ? "UK" : "US";

        // 3. Load active Scoring Rubric
        var rubric = await _db.ScoringRubrics
            .AsNoTracking()
            .Include(r => r.Criteria.OrderBy(c => c.DisplayOrder))
            .Where(r => r.IsActive && r.AssessmentType == "Pronunciation")
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync(ct);

        if (rubric is null)
            throw new InvalidOperationException("Không tìm thấy tiêu chí chấm điểm phát âm (Rubric) đang hoạt động.");

        var enabledCriteria = rubric.Criteria.Where(c => c.IsEnabled).OrderBy(c => c.DisplayOrder).ToList();

        string contentType = string.IsNullOrWhiteSpace(req.ContentType) ? "audio/webm" : req.ContentType;

        // 4. Call AI Assessor (provider called before any database writes)
        var rawResult = await _aiAssessor.AssessAudioAsync(
            req.ApiKey.Trim(),
            effectiveModelCode,
            req.Base64Audio,
            contentType,
            expectedText,
            accent,
            rubric,
            ct);

        // 5. Persist AudioRecording metadata
        var now = DateTimeOffset.UtcNow;
        var recording = new AudioRecordingEntity(
            userId: userId,
            storageKey: $"mem://recordings/{userId}/{Guid.NewGuid():N}",
            contentType: contentType,
            fileSizeBytes: audioBytes.Length,
            durationMilliseconds: req.DurationMilliseconds,
            expiresAt: now.AddDays(30),
            deletionStatus: "Retained");
        _db.AudioRecordings.Add(recording);

        var criterionDtos = new List<CriterionScoreResultDto>();
        var computedScores = new List<(string Code, string Name, string Description, decimal Weight, decimal RawScore, decimal WeightedScore, string Feedback)>();

        foreach (var rubricCrit in enabledCriteria)
        {
            decimal rawScore = 0m;
            string fb = rawResult.IsAssessable ? "Phát âm khá rõ ràng." : "Không nhận diện được giọng nói rõ ràng.";

            if (rawResult.CriterionScores.TryGetValue(rubricCrit.Code, out var parsedCrit))
            {
                rawScore = parsedCrit.RawScore;
                if (!string.IsNullOrWhiteSpace(parsedCrit.Feedback)) fb = parsedCrit.Feedback;
            }

            decimal weightedScore = PronunciationAssessmentEntity.CalculateWeightedScore(rawScore, rubricCrit.Weight);

            computedScores.Add((
                rubricCrit.Code,
                rubricCrit.Name,
                rubricCrit.Description,
                rubricCrit.Weight,
                rawScore,
                weightedScore,
                fb));

            criterionDtos.Add(new CriterionScoreResultDto(
                Code: rubricCrit.Code,
                Name: rubricCrit.Name,
                Description: rubricCrit.Description,
                Weight: rubricCrit.Weight,
                Score: rawScore,
                WeightedScore: weightedScore,
                Feedback: fb
            ));
        }

        decimal? overallScore = PronunciationAssessmentEntity.CalculateOverallScore(
            computedScores.Select(c => c.WeightedScore),
            rawResult.IsAssessable);

        var assessmentEntity = new PronunciationAssessmentEntity(
            userId: userId,
            vocabularyId: req.VocabularyId,
            senseId: req.SenseId,
            targetType: targetType,
            targetId: req.TargetId,
            referenceText: expectedText,
            accent: accent,
            modelCode: effectiveModelCode,
            rubricId: rubric.Id,
            rubricVersion: rubric.Version,
            audioRecordingId: recording.Id,
            recognizedText: rawResult.RecognizedText,
            overallScore: overallScore,
            confidence: rawResult.Confidence,
            status: rawResult.IsAssessable ? "Completed" : "NotAssessable",
            strengthsJson: JsonSerializer.Serialize(rawResult.Strengths),
            improvementsJson: JsonSerializer.Serialize(rawResult.Improvements),
            suggestedPracticeJson: JsonSerializer.Serialize(rawResult.SuggestedPractice),
            completedAt: DateTimeOffset.UtcNow);

        foreach (var cs in computedScores)
        {
            assessmentEntity.AddCriterionScore(
                cs.Code,
                cs.Name,
                cs.Description,
                cs.Weight,
                cs.RawScore,
                cs.WeightedScore,
                cs.Feedback);
        }

        _db.PronunciationAssessments.Add(assessmentEntity);

        // Update User progress with latest pronunciation score
        if (overallScore.HasValue)
        {
            await _assessmentObserver.OnAssessedAsync(userId, req.SenseId, overallScore.Value, ct);
        }

        await _uow.CommitChangesAsync(ct);

        return new PronunciationAssessmentResultDto(
            AssessmentId: assessmentEntity.Id,
            VocabularyId: assessmentEntity.VocabularyId,
            SenseId: assessmentEntity.SenseId,
            TargetType: assessmentEntity.TargetType,
            TargetId: assessmentEntity.TargetId,
            ExpectedText: assessmentEntity.ReferenceText,
            RecognizedText: assessmentEntity.RecognizedText,
            Accent: assessmentEntity.Accent,
            ModelCode: assessmentEntity.ModelCode,
            RubricCode: rubric.Code,
            RubricVersion: rubric.Version,
            OverallScore: assessmentEntity.OverallScore,
            Confidence: assessmentEntity.Confidence,
            IsAssessable: rawResult.IsAssessable,
            Status: assessmentEntity.Status,
            Criteria: criterionDtos,
            Strengths: rawResult.Strengths,
            Improvements: rawResult.Improvements,
            SuggestedPractice: rawResult.SuggestedPractice,
            CreatedAt: assessmentEntity.CreatedAtUtc
        );
    }
}
