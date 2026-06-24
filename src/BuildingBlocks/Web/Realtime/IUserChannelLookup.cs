namespace AMIS.Framework.Web.Realtime;

/// <summary>
/// Adapter the realtime hub uses on connect to discover every channel a user already belongs to, so it can
/// pre-join the matching <c>channel:{id}</c> SignalR groups in <see cref="AppHub.OnConnectedAsync"/>.
/// </summary>
/// <remarks>
/// ⚠️ <b>Coupling:</b> like <see cref="IChannelMembershipChecker"/>, the implementation is provided by a
/// feature module (Chat), never by BuildingBlocks. Without a registered implementation, hub connections fail
/// with a clear DI resolution error. See <c>CHAT-PORT-PLAN.md</c> Phase 0.
/// <para>
/// Implementations must scope the lookup to the caller's tenant explicitly and append any cross-tenant
/// "global" channel ids the user implicitly belongs to.
/// </para>
/// </remarks>
public interface IUserChannelLookup
{
    /// <summary>
    /// Returns the ids of every channel <paramref name="userId"/> currently belongs to (including any implicit
    /// global channels). Used only to pre-join groups; an empty result is valid.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetChannelIdsAsync(Guid userId, CancellationToken cancellationToken = default);
}
