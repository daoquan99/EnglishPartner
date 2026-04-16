using System.Collections.Concurrent;
using EnglishPartner.Modules.AI.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

public sealed class LiveAudioSessionManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ILiveAudioSession> _sessions = new();
    private readonly IServiceProvider _serviceProvider;

    public LiveAudioSessionManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ILiveAudioSession CreateSession(string connectionId)
    {
        var session = _serviceProvider.GetRequiredService<ILiveAudioSession>();
        _sessions.TryAdd(connectionId, session);
        return session;
    }

    public ILiveAudioSession? GetSession(string connectionId)
    {
        _sessions.TryGetValue(connectionId, out var session);
        return session;
    }

    public async Task RemoveSessionAsync(string connectionId)
    {
        if (_sessions.TryRemove(connectionId, out var session))
        {
            await session.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var session in _sessions.Values)
        {
            await session.DisposeAsync();
        }
        _sessions.Clear();
    }
}
