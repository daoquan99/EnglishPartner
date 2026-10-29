using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Domain.Grammar;

public sealed class GrammarExerciseEntity : AggregateRoot<Guid>
{
    public Guid StepId { get; private set; }
    public string? Key { get; private set; }
    public ExerciseType Type { get; private set; }
    public int Difficulty { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Instructions { get; private set; } = string.Empty;
    public string? Focus { get; private set; }
    public string ContentJson { get; private set; } = "{}";
    public string? GradingDefinitionJson { get; private set; }
    public decimal MaxPoints { get; private set; }
    public GradingMode GradingMode { get; private set; }
    public GrammarExerciseSource Source { get; private set; }
    public string? OwnerUserId { get; private set; }
    public string? AuthorUserId { get; private set; }
    public GrammarExerciseVisibility Visibility { get; private set; }
    public GrammarModerationStatus Moderation { get; private set; }
    public string? RejectReason { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public bool IsCore { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public byte[]? RowVersion { get; private set; }

    private GrammarExerciseEntity()
    {
    }

    private GrammarExerciseEntity(
        Guid stepId,
        string? key,
        ExerciseType type,
        int difficulty,
        string title,
        string instructions,
        string contentJson,
        string? gradingDefinitionJson,
        decimal maxPoints,
        GradingMode gradingMode,
        GrammarExerciseSource source,
        string? ownerUserId,
        string? authorUserId,
        GrammarExerciseVisibility visibility,
        GrammarModerationStatus moderation,
        bool isCore,
        string contentHash,
        DateTimeOffset now,
        Guid? id = null,
        string? focus = null)
        : base(id ?? Guid.NewGuid())
    {
        if (stepId == Guid.Empty)
        {
            throw new ArgumentException("StepId cannot be empty.", nameof(stepId));
        }

        StepId = stepId;
        ValidateAndSetContent(type, difficulty, title, instructions, contentJson, gradingDefinitionJson, maxPoints, gradingMode, contentHash, focus);

        if (source == GrammarExerciseSource.Official)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key is required for official exercises.", nameof(key));
            }
            if (key.Trim().Length > 64)
            {
                throw new ArgumentException("Key must not exceed 64 characters.", nameof(key));
            }
            Key = key.Trim();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                if (key.Trim().Length > 64)
                {
                    throw new ArgumentException("Key must not exceed 64 characters.", nameof(key));
                }
                Key = key.Trim();
            }
            else
            {
                Key = null;
            }
        }

        Source = source;
        OwnerUserId = string.IsNullOrWhiteSpace(ownerUserId) ? null : ownerUserId.Trim();
        AuthorUserId = string.IsNullOrWhiteSpace(authorUserId) ? null : authorUserId.Trim();
        Visibility = visibility;
        Moderation = moderation;
        IsCore = isCore;

        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static GrammarExerciseEntity CreateOfficial(
        Guid stepId,
        string key,
        ExerciseType type,
        int difficulty,
        string title,
        string instructions,
        string contentJson,
        string? gradingDefinitionJson,
        decimal maxPoints,
        GradingMode gradingMode,
        bool isCore,
        string contentHash,
        DateTimeOffset now,
        Guid? id = null,
        string? focus = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required for official exercises.", nameof(key));
        }

        if (key.Trim().Length > 64)
        {
            throw new ArgumentException("Key must not exceed 64 characters.", nameof(key));
        }

        return new GrammarExerciseEntity(
            stepId: stepId,
            key: key,
            type: type,
            difficulty: difficulty,
            title: title,
            instructions: instructions,
            contentJson: contentJson,
            gradingDefinitionJson: gradingDefinitionJson,
            maxPoints: maxPoints,
            gradingMode: gradingMode,
            source: GrammarExerciseSource.Official,
            ownerUserId: null,
            authorUserId: null,
            visibility: GrammarExerciseVisibility.Public,
            moderation: GrammarModerationStatus.None,
            isCore: isCore,
            contentHash: contentHash,
            now: now,
            id: id,
            focus: focus);
    }

    public static GrammarExerciseEntity CreateUserOwned(
        Guid stepId,
        string ownerUserId,
        GrammarExerciseSource source,
        GrammarExerciseVisibility visibility,
        ExerciseType type,
        int difficulty,
        string title,
        string instructions,
        string contentJson,
        string? gradingDefinitionJson,
        decimal maxPoints,
        GradingMode gradingMode,
        string contentHash,
        DateTimeOffset now,
        Guid? id = null,
        string? focus = null)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("OwnerUserId is required for user exercises.", nameof(ownerUserId));
        }

        if (source == GrammarExerciseSource.Official)
        {
            throw new ArgumentException("User exercises cannot have Official source.", nameof(source));
        }

        var moderation = visibility == GrammarExerciseVisibility.Public
            ? GrammarModerationStatus.Pending
            : GrammarModerationStatus.None;

        return new GrammarExerciseEntity(
            stepId: stepId,
            key: null,
            type: type,
            difficulty: difficulty,
            title: title,
            instructions: instructions,
            contentJson: contentJson,
            gradingDefinitionJson: gradingDefinitionJson,
            maxPoints: maxPoints,
            gradingMode: gradingMode,
            source: source,
            ownerUserId: ownerUserId,
            authorUserId: ownerUserId,
            visibility: visibility,
            moderation: moderation,
            isCore: false,
            contentHash: contentHash,
            now: now,
            id: id,
            focus: focus);
    }

    public void ChangeVisibility(GrammarExerciseVisibility visibility, DateTimeOffset now)
    {
        if (Source == GrammarExerciseSource.Official)
        {
            throw new InvalidOperationException("Cannot change visibility of official exercises.");
        }

        if (Visibility == visibility)
        {
            return;
        }

        Visibility = visibility;
        if (visibility == GrammarExerciseVisibility.Public)
        {
            Moderation = GrammarModerationStatus.Pending;
            RejectReason = null;
            ReviewedBy = null;
            ReviewedAtUtc = null;
        }
        else
        {
            Moderation = GrammarModerationStatus.None;
            RejectReason = null;
            ReviewedBy = null;
            ReviewedAtUtc = null;
        }

        UpdatedAtUtc = now;
    }

    public void EditContent(
        ExerciseType type,
        int difficulty,
        string title,
        string instructions,
        string contentJson,
        string? gradingDefinitionJson,
        decimal maxPoints,
        GradingMode gradingMode,
        string contentHash,
        DateTimeOffset now,
        string? focus = null)
    {
        ValidateAndSetContent(type, difficulty, title, instructions, contentJson, gradingDefinitionJson, maxPoints, gradingMode, contentHash, focus);

        if (Source != GrammarExerciseSource.Official)
        {
            if (Visibility == GrammarExerciseVisibility.Public)
            {
                Moderation = GrammarModerationStatus.Pending;
                RejectReason = null;
                ReviewedBy = null;
                ReviewedAtUtc = null;
            }
            else
            {
                Moderation = GrammarModerationStatus.None;
            }
        }

        UpdatedAtUtc = now;
    }

    public void Approve(string reviewer, DateTimeOffset now)
    {
        if (Moderation != GrammarModerationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve exercise in status '{Moderation}'. Must be Pending.");
        }

        if (string.IsNullOrWhiteSpace(reviewer))
        {
            throw new ArgumentException("Reviewer is required.", nameof(reviewer));
        }

        Moderation = GrammarModerationStatus.Approved;
        ReviewedBy = reviewer.Trim();
        ReviewedAtUtc = now;
        RejectReason = null;
        UpdatedAtUtc = now;
    }

    public void Reject(string reviewer, string reason, DateTimeOffset now)
    {
        if (Moderation != GrammarModerationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject exercise in status '{Moderation}'. Must be Pending.");
        }

        if (string.IsNullOrWhiteSpace(reviewer))
        {
            throw new ArgumentException("Reviewer is required.", nameof(reviewer));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required when rejecting.", nameof(reason));
        }

        if (reason.Trim().Length > 500)
        {
            throw new ArgumentException("RejectReason must not exceed 500 characters.", nameof(reason));
        }

        Moderation = GrammarModerationStatus.Rejected;
        ReviewedBy = reviewer.Trim();
        ReviewedAtUtc = now;
        RejectReason = reason.Trim();
        UpdatedAtUtc = now;
    }

    public void PromoteToOfficial(string key, bool isCore, DateTimeOffset now)
    {
        if (Moderation != GrammarModerationStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot promote exercise with moderation status '{Moderation}'. Must be Approved.");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required when promoting to official.", nameof(key));
        }

        if (key.Trim().Length > 64)
        {
            throw new ArgumentException("Key must not exceed 64 characters.", nameof(key));
        }

        Key = key.Trim();
        Source = GrammarExerciseSource.Official;
        OwnerUserId = null;
        Visibility = GrammarExerciseVisibility.Public;
        Moderation = GrammarModerationStatus.None;
        RejectReason = null;
        ReviewedBy = null;
        ReviewedAtUtc = null;
        IsCore = isCore;
        UpdatedAtUtc = now;
    }

    public void SetCore(bool isCore)
    {
        if (Source != GrammarExerciseSource.Official)
        {
            throw new InvalidOperationException("Only official exercises can have IsCore modified.");
        }

        IsCore = isCore;
    }

    public bool IsVisibleTo(string? userId)
    {
        if (Source == GrammarExerciseSource.Official)
        {
            return true;
        }

        if (Moderation == GrammarModerationStatus.Approved)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(OwnerUserId) &&
            !string.IsNullOrWhiteSpace(userId) &&
            string.Equals(OwnerUserId, userId, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private void ValidateAndSetContent(
        ExerciseType type,
        int difficulty,
        string title,
        string instructions,
        string contentJson,
        string? gradingDefinitionJson,
        decimal maxPoints,
        GradingMode gradingMode,
        string contentHash,
        string? focus = null)
    {
        if (difficulty < 1 || difficulty > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(difficulty), "Difficulty must be between 1 and 3.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (title.Trim().Length > 200)
        {
            throw new ArgumentException("Title must not exceed 200 characters.", nameof(title));
        }

        if (instructions != null && instructions.Trim().Length > 1000)
        {
            throw new ArgumentException("Instructions must not exceed 1000 characters.", nameof(instructions));
        }

        if (string.IsNullOrWhiteSpace(contentJson))
        {
            throw new ArgumentException("ContentJson is required.", nameof(contentJson));
        }

        if (maxPoints <= 0 || maxPoints > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPoints), "MaxPoints must be greater than 0 and less than or equal to 100.");
        }

        if (string.IsNullOrEmpty(contentHash) ||
            contentHash.Length != 64 ||
            !contentHash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
        {
            throw new ArgumentException("ContentHash must be exactly 64 lowercase hexadecimal characters.", nameof(contentHash));
        }

        string? trimmedFocus = string.IsNullOrWhiteSpace(focus) ? null : focus.Trim();
        if (trimmedFocus != null && trimmedFocus.Length > 200)
        {
            throw new ArgumentException("Focus must not exceed 200 characters.", nameof(focus));
        }

        Type = type;
        Difficulty = difficulty;
        Title = title.Trim();
        Instructions = instructions?.Trim() ?? string.Empty;
        Focus = trimmedFocus;
        ContentJson = contentJson.Trim();
        GradingDefinitionJson = string.IsNullOrWhiteSpace(gradingDefinitionJson) ? null : gradingDefinitionJson.Trim();
        MaxPoints = maxPoints;
        GradingMode = gradingMode;
        ContentHash = contentHash;
    }
}
