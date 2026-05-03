using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.AI.Domain.AiRequestLogs;

/// <summary>
/// Immutable audit log for every AI API call.
/// Tracks input, output, token usage, latency, and caller context.
/// </summary>
public sealed class AiRequestLog : Entity
{
    public string RequestType { get; private set; } = string.Empty;
    public string InputText { get; private set; } = string.Empty;
    public string OutputText { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = string.Empty;
    public int? InputTokens { get; private set; }
    public int? OutputTokens { get; private set; }
    public long LatencyMs { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? UserId { get; private set; }

    private AiRequestLog() { }

    public static AiRequestLog Create(
        string requestType,
        string inputText,
        string outputText,
        string modelName,
        long latencyMs,
        bool isSuccess,
        string? errorMessage = null,
        Guid? userId = null,
        int? inputTokens = null,
        int? outputTokens = null)
    {
        return new AiRequestLog
        {
            Id = Guid.NewGuid(),
            RequestType = requestType,
            InputText = inputText,
            OutputText = outputText,
            ModelName = modelName,
            LatencyMs = latencyMs,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            UserId = userId,
            InputTokens = inputTokens,
            OutputTokens = outputTokens
        };
    }
}
