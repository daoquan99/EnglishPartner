using System.Collections.Concurrent;
using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class PronunciationSynthesisLock : IPronunciationSynthesisLock
{
    private sealed class LockEntry
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int WaiterCount = 0;
    }

    private readonly ConcurrentDictionary<string, LockEntry> _locks = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IDisposable> AcquireLockAsync(string key, CancellationToken ct = default)
    {
        LockEntry entry;
        lock (_locks)
        {
            entry = _locks.GetOrAdd(key, _ => new LockEntry());
            Interlocked.Increment(ref entry.WaiterCount);
        }

        try
        {
            await entry.Semaphore.WaitAsync(ct);
        }
        catch
        {
            lock (_locks)
            {
                if (Interlocked.Decrement(ref entry.WaiterCount) == 0)
                {
                    _locks.TryRemove(key, out _);
                    entry.Semaphore.Dispose();
                }
            }
            throw;
        }

        return new Releaser(this, key, entry);
    }

    private void Release(string key, LockEntry entry)
    {
        lock (_locks)
        {
            entry.Semaphore.Release();
            if (Interlocked.Decrement(ref entry.WaiterCount) == 0)
            {
                _locks.TryRemove(key, out _);
                entry.Semaphore.Dispose();
            }
        }
    }

    private sealed class Releaser(PronunciationSynthesisLock parent, string key, LockEntry entry) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                parent.Release(key, entry);
            }
        }
    }
}
