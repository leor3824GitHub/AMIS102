namespace AMIS.Framework.Web.Realtime;

/// <summary>
/// Tracks which users currently have at least one live realtime connection.
/// </summary>
/// <remarks>
/// The default <see cref="PresenceTracker"/> is an in-memory, <b>per-host</b> count — correct only for a
/// single replica. Multi-replica presence needs a shared (e.g. Redis-backed) store; the SignalR Redis
/// backplane fans out <i>messages</i> across replicas but does not aggregate the presence <i>count</i>.
/// </remarks>
public interface IPresenceTracker
{
    /// <summary>
    /// Records a new connection for <paramref name="userId"/>. Returns <c>true</c> if this was the user's
    /// first connection (i.e. they just came online), so the caller can broadcast a presence change.
    /// </summary>
    bool Connect(Guid userId);

    /// <summary>
    /// Records that a connection for <paramref name="userId"/> closed. Returns <c>true</c> if it was the
    /// user's last connection (i.e. they just went offline).
    /// </summary>
    bool Disconnect(Guid userId);

    /// <summary>Returns <c>true</c> when <paramref name="userId"/> has at least one live connection.</summary>
    bool IsOnline(Guid userId);

    /// <summary>Snapshot of all users currently online on this host.</summary>
    IReadOnlyCollection<Guid> OnlineUsers();
}
