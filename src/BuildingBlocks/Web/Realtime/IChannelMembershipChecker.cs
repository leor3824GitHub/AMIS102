namespace AMIS.Framework.Web.Realtime;

/// <summary>
/// Adapter the realtime hub uses to decide whether a user may join a channel group.
/// </summary>
/// <remarks>
/// ⚠️ <b>Coupling:</b> <see cref="AppHub"/> takes a hard runtime dependency on this interface, but the
/// implementation is supplied by a feature module (the Chat module), not by BuildingBlocks. BuildingBlocks
/// cannot reference the module, so realtime is effectively unbootable for actual hub connections unless a
/// module registers an <see cref="IChannelMembershipChecker"/>. If none is registered, hub connections fail
/// with a clear DI resolution error. See <c>CHAT-PORT-PLAN.md</c> Phase 0.
/// <para>
/// Implementations must scope membership lookups to the caller's tenant explicitly (the ambient tenant
/// filter is not reliable inside the hub) and special-case any cross-tenant "global" channels.
/// </para>
/// </remarks>
public interface IChannelMembershipChecker
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="userId"/> is a member of <paramref name="channelId"/>.
    /// </summary>
    Task<bool> IsMemberAsync(Guid channelId, Guid userId, CancellationToken cancellationToken = default);
}
