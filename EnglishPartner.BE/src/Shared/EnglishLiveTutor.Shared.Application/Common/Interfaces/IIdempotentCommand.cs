namespace EnglishLiveTutor.Application.Common.Interfaces;

/// <summary>
/// Marker interface indicating that the command supports idempotency via an IdempotencyKey.
/// </summary>
public interface IIdempotentCommand
{
    string? IdempotencyKey { get; }
}
