using EnglishLiveTutor.Application.Features.Grammar.Models;

namespace EnglishLiveTutor.Application.Features.Grammar.Ports;

public interface IGrammarExerciseAiGenerator
{
    Task<string> GenerateRawAsync(string prompt, GrammarAiExecutionConfig config, CancellationToken ct = default);
}
