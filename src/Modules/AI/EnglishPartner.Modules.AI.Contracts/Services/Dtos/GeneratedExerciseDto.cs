namespace EnglishPartner.Modules.AI.Contracts.Services.Dtos;

/// <summary>AI-generated exercise ready to be persisted.</summary>
public sealed record GeneratedExerciseDto(
    string Title,
    string Instruction,
    string? Content,
    string CorrectAnswer,
    string? Explanation,
    List<GeneratedExerciseOptionDto> Options);

public sealed record GeneratedExerciseOptionDto(
    string Text,
    bool IsCorrect);
