using System.Collections.Concurrent;

public static class GameSessionManager
{
    private static ConcurrentDictionary<string, GameSession> _sessions = new();

    public static bool TryGetSession(string gameId, out GameSession session) =>
        _sessions.TryGetValue(gameId, out session);

    public static bool AddSession(string gameId, GameSession session) =>
        _sessions.TryAdd(gameId, session);

    public static GameSession? GetSession(string gameId) =>
        _sessions.TryGetValue(gameId, out var session) ? session : null;

    public static void RemoveSession(string gameId) =>
        _sessions.TryRemove(gameId, out _);
}
