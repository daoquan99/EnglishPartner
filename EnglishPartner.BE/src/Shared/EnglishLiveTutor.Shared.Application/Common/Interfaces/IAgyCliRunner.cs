namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface IAgyCliRunner
{
    bool IsEnabled { get; }
    IReadOnlyCollection<string> AllowedModels { get; }

    Task<string> ExecutePromptAsync(
        string prompt,
        string? modelCode = null,
        CancellationToken ct = default);

    bool IsAgyModel(string? modelCode);
    string NormalizeModel(string? modelCode);
}
