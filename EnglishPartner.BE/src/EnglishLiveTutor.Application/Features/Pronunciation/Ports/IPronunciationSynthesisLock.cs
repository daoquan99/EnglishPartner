namespace EnglishLiveTutor.Application.Features.Pronunciation.Ports;

public interface IPronunciationSynthesisLock
{
    Task<IDisposable> AcquireLockAsync(string key, CancellationToken ct = default);
}
