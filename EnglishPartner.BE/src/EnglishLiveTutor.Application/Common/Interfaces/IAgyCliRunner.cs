namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface IAgyCliRunner
{
    Task<string> ExecutePromptAsync(
        string prompt,
        string? modelCode = null,
        CancellationToken ct = default);

    bool IsAgyModel(string? modelCode);
    string NormalizeModel(string? modelCode);
}
