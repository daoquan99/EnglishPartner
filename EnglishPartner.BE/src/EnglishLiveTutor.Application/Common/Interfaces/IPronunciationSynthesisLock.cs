namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface IPronunciationSynthesisLock
{
    Task<IDisposable> AcquireLockAsync(string key, CancellationToken ct = default);
}
