using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface ILessonCandidateValidator
{
    Task<LessonCandidateValidationResultDto> ValidateCandidateAsync(JsonNode? candidateRoot, CancellationToken ct = default);
}
