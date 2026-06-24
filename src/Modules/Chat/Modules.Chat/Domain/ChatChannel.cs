using AMIS.Framework.Core.Domain;
using AMIS.Modules.Chat.Contracts.v1;

namespace AMIS.Modules.Chat.Domain;

/// <summary>
/// Channel aggregate root. Office channels/DMs carry the owning <see cref="TenantId"/>; the single seeded
/// global channel has <see cref="TenantId"/> = null and <see cref="Scope"/> = <see cref="ChannelScope.Global"/>.
/// Soft-deletable (Archive/Restore flip the flag — never EF-removed).
/// </summary>
public sealed class ChatChannel : AggregateRoot<Guid>, ISoftDeletable
{
    private readonly List<ChannelMember> _members = [];

    private ChatChannel() { } // EF

    /// <summary>Owning office (tenant identifier). Null for the global channel.</summary>
    public string? TenantId { get; private set; }

    public ChannelType Type { get; private set; }

    public ChannelScope Scope { get; private set; }

    public string? Name { get; private set; }

    public string? Topic { get; private set; }

    /// <summary>Sorted <c>"{lo}:{hi}"</c> user-id pair for 1:1 DM find-or-create. Null for non-DM channels.</summary>
    public string? DirectKey { get; private set; }

    public string CreatedBy { get; private set; } = default!;

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public Guid? LastMessageId { get; private set; }

    public DateTimeOffset? LastMessageAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public IReadOnlyCollection<ChannelMember> Members => _members;

    /// <summary>Office-scoped named channel.</summary>
    public static ChatChannel CreateChannel(string tenantId, string name, string? topic, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ChatChannel
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Type = ChannelType.Named,
            Scope = ChannelScope.Office,
            Name = name.Trim(),
            Topic = topic?.Trim(),
            CreatedBy = createdBy,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Office-scoped 1:1 direct message between two users (both added as members).</summary>
    public static ChatChannel CreateDirect(string tenantId, string userA, string userB, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userA);
        ArgumentException.ThrowIfNullOrWhiteSpace(userB);

        var channel = new ChatChannel
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Type = ChannelType.Direct,
            Scope = ChannelScope.Office,
            DirectKey = BuildDirectKey(userA, userB),
            CreatedBy = createdBy,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };

        channel.AddMember(userA, ChannelMemberRole.Member);
        channel.AddMember(userB, ChannelMemberRole.Member);
        return channel;
    }

    /// <summary>Office-scoped multi-user group DM.</summary>
    public static ChatChannel CreateGroupDm(string tenantId, string name, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return new ChatChannel
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Type = ChannelType.GroupDm,
            Scope = ChannelScope.Office,
            Name = name?.Trim(),
            CreatedBy = createdBy,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// The single NFA-wide global channel. Uses the fixed <see cref="ChatModuleConstants.GlobalChannelId"/>,
    /// <see cref="TenantId"/> = null, and has NO member rows (implicit membership for every authenticated user).
    /// </summary>
    public static ChatChannel CreateGlobal(string name, string? topic)
    {
        return new ChatChannel
        {
            Id = ChatModuleConstants.GlobalChannelId,
            TenantId = null,
            Type = ChannelType.Named,
            Scope = ChannelScope.Global,
            Name = name,
            Topic = topic,
            CreatedBy = "system",
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    public ChannelMember AddMember(string userId, ChannelMemberRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var existing = _members.Find(m => string.Equals(m.UserId, userId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var member = ChannelMember.Create(Id, userId, role, TenantId);
        _members.Add(member);
        return member;
    }

    /// <summary>Removes a member by user id. Returns false if the user was not a member (idempotent).</summary>
    public bool RemoveMember(string userId)
    {
        var existing = _members.Find(m => string.Equals(m.UserId, userId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            return false;
        }

        _members.Remove(existing);
        return true;
    }

    public void Rename(string name, string? topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Topic = topic?.Trim();
    }

    public void TouchLastMessage(Guid messageId, DateTimeOffset atUtc)
    {
        LastMessageId = messageId;
        LastMessageAtUtc = atUtc;
    }

    public void Archive(string by)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = by;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
    }

    /// <summary>Builds the order-independent DM key so (A,B) and (B,A) resolve to the same channel.</summary>
    public static string BuildDirectKey(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? $"{a}:{b}" : $"{b}:{a}";
}
