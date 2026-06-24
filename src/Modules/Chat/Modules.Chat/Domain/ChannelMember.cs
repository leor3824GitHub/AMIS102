using AMIS.Framework.Core.Domain;
using AMIS.Modules.Chat.Contracts.v1;

namespace AMIS.Modules.Chat.Domain;

/// <summary>
/// Explicit channel membership row. Not tenant-filtered — membership IS the authorization boundary.
/// The global channel has no member rows (implicit membership).
/// </summary>
public sealed class ChannelMember : BaseEntity<Guid>
{
    private ChannelMember() { } // EF

    public Guid ChannelId { get; private set; }

    public string UserId { get; private set; } = default!;

    public ChannelMemberRole Role { get; private set; }

    /// <summary>Denormalized owning office for display/reporting. Never used as a query filter.</summary>
    public string? TenantId { get; private set; }

    public DateTimeOffset JoinedOnUtc { get; private set; }

    public Guid? LastReadMessageId { get; private set; }

    public DateTimeOffset? LastReadOnUtc { get; private set; }

    internal static ChannelMember Create(Guid channelId, string userId, ChannelMemberRole role, string? tenantId) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ChannelId = channelId,
            UserId = userId,
            Role = role,
            TenantId = tenantId,
            JoinedOnUtc = DateTimeOffset.UtcNow,
        };

    public void MarkRead(Guid messageId)
    {
        LastReadMessageId = messageId;
        LastReadOnUtc = DateTimeOffset.UtcNow;
    }
}
