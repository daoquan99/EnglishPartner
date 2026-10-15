using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Vocabulary;

/// <summary>
/// Independently persisted append-only audit record for AI vocabulary generation runs.
/// </summary>
public sealed class VocabularyGenerationAuditEntity : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = "default-learner";
    public string ModelCode { get; private set; } = string.Empty;
    public string PromptVersion { get; private set; } = "VOCAB_GEN_V1";
    public int RequestedCount { get; private set; }
    public int AcceptedCount { get; private set; }
    public int RejectedCount { get; private set; }
    public string Status { get; private set; } = "Pending";
    public string? ErrorMessage { get; private set; }

    private VocabularyGenerationAuditEntity()
    {
    }

    public VocabularyGenerationAuditEntity(
        string userId,
        string modelCode,
        string promptVersion,
        int requestedCount,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        UserId = string.IsNullOrWhiteSpace(userId) ? "default-learner" : userId.Trim();
        ModelCode = modelCode?.Trim() ?? string.Empty;
        PromptVersion = string.IsNullOrWhiteSpace(promptVersion) ? "VOCAB_GEN_V1" : promptVersion.Trim();
        RequestedCount = requestedCount;
        Status = "Pending";
    }

    public void MarkCompleted(int acceptedCount, int rejectedCount)
    {
        AcceptedCount = acceptedCount;
        RejectedCount = rejectedCount;
        Status = "Completed";
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
    }
}
