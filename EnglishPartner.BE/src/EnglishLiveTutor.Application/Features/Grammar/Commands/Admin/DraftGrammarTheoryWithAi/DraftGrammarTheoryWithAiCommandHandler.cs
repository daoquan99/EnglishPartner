using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Ports;
using EnglishLiveTutor.Application.Features.Grammar.Rules;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.DraftGrammarTheoryWithAi;

public sealed class DraftGrammarTheoryWithAiCommandHandler
    : ICommandHandler<DraftGrammarTheoryWithAiCommand, DraftGrammarTheoryWithAiResponse>
{
    private static readonly Dictionary<string, int> CefrRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A1"] = 1,
        ["A2"] = 2,
        ["B1"] = 3,
        ["B2"] = 4,
        ["C1"] = 5,
        ["C2"] = 6
    };

    private readonly IAppDbContext _db;
    private readonly IGrammarExerciseAiGenerator _aiGenerator;

    public DraftGrammarTheoryWithAiCommandHandler(
        IAppDbContext db,
        IGrammarExerciseAiGenerator aiGenerator)
    {
        _db = db;
        _aiGenerator = aiGenerator;
    }

    public async Task<DraftGrammarTheoryWithAiResponse> HandleAsync(
        DraftGrammarTheoryWithAiCommand command,
        CancellationToken ct = default)
    {
        string normalizedCode = command.StepCode.Trim().ToUpperInvariant();
        var step = await _db.GrammarSteps.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == normalizedCode && !s.IsDeleted, ct);

        if (step == null)
        {
            throw new NotFoundException($"Step '{command.StepCode}' không tồn tại.");
        }

        // Fetch prior steps to enforce progressive grammar scope
        var allSteps = await _db.GrammarSteps.AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.CefrLevel)
            .ThenBy(s => s.Order)
            .ToListAsync(ct);

        var orderedSteps = allSteps
            .OrderBy(s => CefrRank.TryGetValue(s.CefrLevel, out var r) ? r : 99)
            .ThenBy(s => s.Order)
            .ToList();

        int currentIndex = orderedSteps.FindIndex(s => s.Id == step.Id);
        var priorStepTitles = currentIndex > 0
            ? orderedSteps.Take(currentIndex).Select(s => s.TitleEn).ToList()
            : new List<string>();

        string priorText = priorStepTitles.Count > 0 ? string.Join(", ", priorStepTitles) : "None (this is the introductory step)";

        string prompt = $@"You are an expert English grammar teacher and curriculum designer for Vietnamese learners.
Draft the comprehensive grammar theory for ONE step of a CEFR-aligned grammar course.

## Step Information
- Code: {step.Code} (CEFR {step.CefrLevel})
- English Topic: {step.TitleEn}
- Vietnamese Topic: {step.TitleVi}
- Summary: {step.SummaryVi}

## Grammar Scope
- Prior learned grammar: {priorText}
- Scope rule: Only use vocabulary and grammar up to CEFR {step.CefrLevel}. All explanations MUST be in clear Vietnamese. All examples MUST be natural English with Vietnamese translations.

## Required Output Schema (TheoryJson)
Return ONLY a valid JSON object matching this exact schema, without markdown code fence or extra text:
{{
  ""objectives"": [
    ""Mục tiêu 1 bằng tiếng Việt..."",
    ""Mục tiêu 2...""
  ],
  ""sections"": [
    {{
      ""heading"": ""Tiêu đề phần 1"",
      ""explanationVi"": ""Giải thích chi tiết bằng tiếng Việt..."",
      ""formulas"": [
        {{ ""label"": ""Khẳng định"", ""pattern"": ""S + V(s/es) + O"" }}
      ],
      ""examples"": [
        {{ ""en"": ""She works every day."", ""vi"": ""Cô ấy làm việc mỗi ngày."", ""highlight"": ""works"" }}
      ],
      ""notes"": [
        ""Lưu ý quan trọng...""
      ]
    }}
  ],
  ""commonMistakes"": [
    {{
      ""wrong"": ""He go to school"",
      ""right"": ""He goes to school"",
      ""explanationVi"": ""Chủ ngữ ngôi thứ ba số ít cần thêm -es vào động từ 'go'.""
    }}
  ],
  ""tips"": [
    ""Mẹo ghi nhớ 1...""
  ],
  ""summary"": [
    ""Tóm tắt điểm chính 1...""
  ]
}}";

        // Call AI generator outside transaction
        string rawResponse = await _aiGenerator.GenerateRawAsync(
            prompt,
            command.Request.AiConfig ?? new GrammarAiExecutionConfig(),
            ct);

        string cleanJson = GrammarExerciseOutputProcessor.StripCodeFence(rawResponse);

        // Validate draft via GrammarTheoryValidator
        var issues = GrammarTheoryValidator.Validate(cleanJson, step.IsLevelReview);

        JsonElement theoryElement;
        try
        {
            using var doc = JsonDocument.Parse(cleanJson);
            theoryElement = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            issues.Add(new ValidationIssueDto("$", "Error", $"Malformed JSON output from AI: {ex.Message}"));
            using var emptyDoc = JsonDocument.Parse("{}");
            theoryElement = emptyDoc.RootElement.Clone();
        }

        // Return without persisting to DB
        return new DraftGrammarTheoryWithAiResponse(theoryElement, issues);
    }
}
