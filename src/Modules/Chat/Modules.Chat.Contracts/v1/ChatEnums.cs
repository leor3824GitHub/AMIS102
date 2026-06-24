namespace AMIS.Modules.Chat.Contracts.v1;

/// <summary>The kind of channel. Orthogonal to <see cref="ChannelScope"/>.</summary>
public enum ChannelType
{
    /// <summary>1:1 direct message between exactly two users.</summary>
    Direct = 0,

    /// <summary>Multi-user direct message (private, member-listed).</summary>
    GroupDm = 1,

    /// <summary>Named channel (office-wide or, for the seeded global channel, NFA-wide).</summary>
    Named = 2,
}

/// <summary>
/// The tenancy reach of a channel. Orthogonal to <see cref="ChannelType"/> — the seeded global
/// channel is <c>Type=Named, Scope=Global</c>.
/// </summary>
public enum ChannelScope
{
    /// <summary>Visible only within the owning office (tenant). The default for every user-created channel/DM.</summary>
    Office = 0,

    /// <summary>Visible to every authenticated user across all offices. v1 ships exactly one seeded global channel.</summary>
    Global = 1,
}

/// <summary>A member's role within a channel.</summary>
public enum ChannelMemberRole
{
    Member = 0,
    Admin = 1,
}
