using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Rules;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Application.Features.Grammar.ContentPacks;

public sealed class GrammarContentPackImporter
{
    private static readonly Regex GrammarStepCodeRegex = new(@"^(A1|A2|B1|B2|C1|C2)-(\d{2})$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IGrammarDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IGrammarContentPackSource _source;
    private readonly TimeProvider _clock;
    private readonly ILogger<GrammarContentPackImporter>? _logger;

    public GrammarContentPackImporter(
        IGrammarDbContext db,
        IUnitOfWork uow,
        IGrammarContentPackSource source,
        TimeProvider? clock = null,
        ILogger<GrammarContentPackImporter>? logger = null)
    {
        _db = db;
        _uow = uow;
        _source = source;
        _clock = clock ?? TimeProvider.System;
        _logger = logger;
    }

    public async Task<GrammarContentPackImportResult> ImportAsync(CancellationToken ct = default)
    {
        int stepsCreated = 0;
        int stepsUpdated = 0;
        int stepsSkipped = 0;
        int exercisesCreated = 0;
        int exercisesSkipped = 0;
        var errors = new List<GrammarImportIssue>();

        var docs = _source.LoadAll();
        if (docs == null || docs.Count == 0)
        {
            return new GrammarContentPackImportResult(0, 0, 0, 0, 0, errors);
        }

        var now = _clock.GetUtcNow();

        // Load existing steps into memory for quick lookup and local tracking (ignoring soft-delete filters)
        var existingStepsList = await _db.GrammarSteps.IgnoreQueryFilters().ToListAsync(ct);
        var stepsByCode = existingStepsList.ToDictionary(s => s.Code, s => s, StringComparer.OrdinalIgnoreCase);

        // Preload existing official exercise keys and content hashes (ignoring soft-delete filters)
        var existingOfficialExercises = await _db.GrammarExercises
            .IgnoreQueryFilters()
            .Where(e => e.Source == GrammarExerciseSource.Official)
            .Select(e => new { e.StepId, e.Key, e.ContentHash })
            .ToListAsync(ct);

        var existingKeysByStep = new Dictionary<Guid, HashSet<string>>();
        var existingHashesByStep = new Dictionary<Guid, HashSet<string>>();

        foreach (var ex in existingOfficialExercises)
        {
            if (!existingKeysByStep.TryGetValue(ex.StepId, out var keySet))
            {
                keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                existingKeysByStep[ex.StepId] = keySet;
            }

            if (!string.IsNullOrWhiteSpace(ex.Key))
            {
                keySet.Add(ex.Key);
            }

            if (!existingHashesByStep.TryGetValue(ex.StepId, out var hashSet))
            {
                hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                existingHashesByStep[ex.StepId] = hashSet;
            }

            if (!string.IsNullOrWhiteSpace(ex.ContentHash))
            {
                hashSet.Add(ex.ContentHash);
            }
        }

        foreach (var doc in docs)
        {
            if (string.IsNullOrWhiteSpace(doc.Json))
            {
                continue;
            }

            GrammarContentPackDto? packDto;
            try
            {
                packDto = JsonSerializer.Deserialize<GrammarContentPackDto>(doc.Json, JsonOptions);
            }
            catch (JsonException ex)
            {
                errors.Add(new GrammarImportIssue(null, null, $"Document '{doc.Name}' contains invalid JSON: {ex.Message}"));
                continue;
            }

            if (packDto?.Steps == null || packDto.Steps.Count == 0)
            {
                continue;
            }

            string packLevel = packDto.Level?.Trim().ToUpperInvariant() ?? string.Empty;

            foreach (var stepDto in packDto.Steps)
            {
                string rawCode = stepDto.Code?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(rawCode))
                {
                    stepsSkipped++;
                    errors.Add(new GrammarImportIssue(null, null, "Step is missing required 'code'."));
                    continue;
                }

                // 7. Check pack.level == prefix(code) and order matches number in code before branching
                var codeMatch = GrammarStepCodeRegex.Match(rawCode);
                if (!codeMatch.Success)
                {
                    stepsSkipped++;
                    errors.Add(new GrammarImportIssue(rawCode, null, $"Step code '{rawCode}' does not match pattern '^(A1|A2|B1|B2|C1|C2)-\\d{{2}}$'."));
                    continue;
                }

                var codePrefix = codeMatch.Groups[1].Value;
                var codeNumber = int.Parse(codeMatch.Groups[2].Value);

                if (!string.Equals(packLevel, codePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    stepsSkipped++;
                    errors.Add(new GrammarImportIssue(rawCode, null, $"Pack level '{packLevel}' does not match step code prefix '{codePrefix}'."));
                    continue;
                }

                if (stepDto.Order != codeNumber)
                {
                    stepsSkipped++;
                    errors.Add(new GrammarImportIssue(rawCode, null, $"Step order '{stepDto.Order}' does not match number in code '{codeNumber}'."));
                    continue;
                }

                // 4. Soft-deleted step check: do not recreate, do not update, count as skipped (no error)
                if (stepsByCode.TryGetValue(rawCode, out var existingStep) && existingStep.IsDeleted)
                {
                    stepsSkipped++;
                    continue;
                }

                // 1. Validate theory
                string theoryJson = stepDto.Theory.ValueKind != JsonValueKind.Undefined && stepDto.Theory.ValueKind != JsonValueKind.Null
                    ? stepDto.Theory.GetRawText()
                    : string.Empty;

                var theoryIssues = GrammarTheoryValidator.Validate(theoryJson, stepDto.IsLevelReview);
                if (theoryIssues.Any(i => i.Severity == "Error"))
                {
                    stepsSkipped++;
                    foreach (var issue in theoryIssues.Where(i => i.Severity == "Error"))
                    {
                        errors.Add(new GrammarImportIssue(rawCode, null, $"Theory validation error ({issue.Path}): {issue.Message}"));
                    }
                    continue;
                }

                // 2. Create or update step
                GrammarStepEntity stepEntity;
                if (existingStep == null)
                {
                    try
                    {
                        stepEntity = GrammarStepEntity.Create(
                            code: rawCode,
                            cefrLevel: packLevel,
                            order: stepDto.Order,
                            titleVi: stepDto.TitleVi,
                            titleEn: stepDto.TitleEn,
                            summaryVi: stepDto.SummaryVi,
                            theoryJson: theoryJson,
                            theoryVersion: stepDto.TheoryVersion,
                            isLevelReview: stepDto.IsLevelReview,
                            isPublished: true);

                        _db.GrammarSteps.Add(stepEntity);
                        stepsByCode[rawCode] = stepEntity;
                        stepsCreated++;
                    }
                    catch (Exception ex)
                    {
                        stepsSkipped++;
                        errors.Add(new GrammarImportIssue(rawCode, null, $"Failed to instantiate step '{rawCode}': {ex.Message}"));
                        continue;
                    }
                }
                else
                {
                    try
                    {
                        stepEntity = existingStep;
                        bool metadataChanged = !string.Equals(existingStep.TitleVi, stepDto.TitleVi?.Trim(), StringComparison.Ordinal)
                            || !string.Equals(existingStep.TitleEn, stepDto.TitleEn?.Trim(), StringComparison.Ordinal)
                            || !string.Equals(existingStep.SummaryVi, stepDto.SummaryVi?.Trim(), StringComparison.Ordinal);

                        if (metadataChanged)
                        {
                            existingStep.UpdateMetadata(stepDto.TitleVi, stepDto.TitleEn, stepDto.SummaryVi, isPublished: existingStep.IsPublished);
                        }

                        bool theoryChanged = existingStep.ApplyPackTheory(theoryJson, stepDto.TheoryVersion);

                        if (metadataChanged || theoryChanged)
                        {
                            stepsUpdated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        stepsSkipped++;
                        errors.Add(new GrammarImportIssue(rawCode, null, $"Failed to update step '{rawCode}': {ex.Message}"));
                        continue;
                    }
                }

                // 3. Process exercises for this step
                if (stepDto.Exercises != null && stepDto.Exercises.Count > 0)
                {
                    if (!existingKeysByStep.TryGetValue(stepEntity.Id, out var keySet))
                    {
                        keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        existingKeysByStep[stepEntity.Id] = keySet;
                    }

                    if (!existingHashesByStep.TryGetValue(stepEntity.Id, out var hashSet))
                    {
                        hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        existingHashesByStep[stepEntity.Id] = hashSet;
                    }

                    foreach (var exDto in stepDto.Exercises)
                    {
                        string exKey = exDto.Key?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(exKey))
                        {
                            exercisesSkipped++;
                            errors.Add(new GrammarImportIssue(stepEntity.Code, null, "Exercise is missing 'key'."));
                            continue;
                        }

                        if (!Enum.TryParse<ExerciseType>(exDto.Type, ignoreCase: true, out var exerciseType))
                        {
                            exercisesSkipped++;
                            errors.Add(new GrammarImportIssue(stepEntity.Code, exKey, $"Unsupported ExerciseType '{exDto.Type}'."));
                            continue;
                        }

                        if (!Enum.TryParse<GradingMode>(exDto.GradingMode, ignoreCase: true, out var gradingMode))
                        {
                            exercisesSkipped++;
                            errors.Add(new GrammarImportIssue(stepEntity.Code, exKey, $"Unsupported GradingMode '{exDto.GradingMode}'."));
                            continue;
                        }

                        string contentJson = exDto.Content.ValueKind != JsonValueKind.Undefined && exDto.Content.ValueKind != JsonValueKind.Null
                            ? exDto.Content.GetRawText()
                            : "{}";

                        string? gradingJson = exDto.Grading.HasValue && exDto.Grading.Value.ValueKind != JsonValueKind.Undefined && exDto.Grading.Value.ValueKind != JsonValueKind.Null
                            ? exDto.Grading.Value.GetRawText()
                            : null;

                        var ruleIssues = GrammarExerciseRules.Validate(exerciseType, contentJson, gradingJson, exDto.MaxPoints, gradingMode);
                        if (ruleIssues.Any(i => i.Severity == "Error"))
                        {
                            exercisesSkipped++;
                            foreach (var err in ruleIssues.Where(i => i.Severity == "Error"))
                            {
                                errors.Add(new GrammarImportIssue(stepEntity.Code, exKey, $"Exercise rule error ({err.Path}): {err.Message}"));
                            }
                            continue;
                        }

                        string contentHash = GrammarContentHasher.ComputeHash(exerciseType, contentJson, gradingJson);

                        if (keySet.Contains(exKey) || hashSet.Contains(contentHash))
                        {
                            // Skip duplicates (including soft-deleted official exercises preloaded)
                            exercisesSkipped++;
                            continue;
                        }

                        try
                        {
                            var exerciseEntity = GrammarExerciseEntity.CreateOfficial(
                                stepId: stepEntity.Id,
                                key: exKey,
                                type: exerciseType,
                                difficulty: exDto.Difficulty,
                                title: exDto.Title,
                                instructions: exDto.Instructions,
                                contentJson: contentJson,
                                gradingDefinitionJson: gradingJson,
                                maxPoints: exDto.MaxPoints,
                                gradingMode: gradingMode,
                                isCore: exDto.IsCore,
                                contentHash: contentHash,
                                now: now,
                                focus: exDto.Focus);

                            _db.GrammarExercises.Add(exerciseEntity);
                            keySet.Add(exKey);
                            hashSet.Add(contentHash);
                            exercisesCreated++;
                        }
                        catch (Exception ex)
                        {
                            exercisesSkipped++;
                            errors.Add(new GrammarImportIssue(stepEntity.Code, exKey, $"Failed to create exercise '{exKey}': {ex.Message}"));
                        }
                    }
                }
            }
        }

        await _uow.CommitChangesAsync(ct);

        return new GrammarContentPackImportResult(
            stepsCreated,
            stepsUpdated,
            stepsSkipped,
            exercisesCreated,
            exercisesSkipped,
            errors);
    }
}
