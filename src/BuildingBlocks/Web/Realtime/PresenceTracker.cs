using System.Collections.Concurrent;

namespace AMIS.Framework.Web.Realtime;

/// <summary>
/// In-memory, per-host presence tracker backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/> of
/// userId → live-connection count. Single-replica only — see <see cref="IPresenceTracker"/>.
/// </summary>
public sealed class PresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, int> _connectionCounts = new();

    /// <inheritdoc />
    public bool Connect(Guid userId)
    {
        // AddOrUpdate returns the value actually stored; the add value is a constant and the update delegate
        // is a pure function of the prior count, so the result is race-safe even under concurrent connects.
        var count = _connectionCounts.AddOrUpdate(userId, 1, static (_, existing) => existing + 1);
        return count == 1;
    }

    /// <inheritdoc />
    public bool Disconnect(Guid userId)
    {
        var count = _connectionCounts.AddOrUpdate(userId, 0, static (_, existing) => existing - 1);
        if (count <= 0)
        {
            // Conditional remove: only drops the entry if it is still the zero we just observed.
            _connectionCounts.TryRemove(new KeyValuePair<Guid, int>(userId, count));
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsOnline(Guid userId) => _connectionCounts.TryGetValue(userId, out var count) && count > 0;

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> OnlineUsers() => _connectionCounts.Keys.ToArray();
}
