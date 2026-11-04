namespace EnglishLiveTutor.Application.Common.Models;

public sealed record LessonAiExecutionConfigDto(
    string? ApiKey = null,
    string? AssessmentModelCode = null,
    string? Provider = null
);
