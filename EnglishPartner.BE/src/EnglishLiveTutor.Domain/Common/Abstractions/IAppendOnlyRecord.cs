namespace EnglishLiveTutor.Domain.Common.Abstractions;

/// <summary>
/// Marker interface for historical or audit records whose business lifecycle is append-only.
/// Existing SQL tables retain soft-delete columns for schema compatibility.
/// </summary>
public interface IAppendOnlyRecord
{
}
