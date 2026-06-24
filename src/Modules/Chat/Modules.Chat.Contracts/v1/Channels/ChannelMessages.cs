using AMIS.Modules.Chat.Contracts.v1.DTOs;
using Mediator;

namespace AMIS.Modules.Chat.Contracts.v1.Channels;

/// <summary>Create an office-scoped named channel, optionally with an initial member set.</summary>
public sealed record CreateChannelCommand(
    string Name,
    string? Topic = null,
    IReadOnlyList<string>? MemberUserIds = null) : ICommand<ChannelDto>;

/// <summary>
/// Find the caller's existing 1:1 DM with <paramref name="OtherUserId"/>, or create it.
/// Idempotent under concurrency via the unique <c>(TenantId, DirectKey)</c> index.
/// </summary>
public sealed record FindOrCreateDmCommand(string OtherUserId) : ICommand<ChannelDto>;

/// <summary>List every channel the caller belongs to (including the implicit global channel), newest activity first.</summary>
public sealed record ListMyChannelsQuery : IQuery<IReadOnlyList<ChannelDto>>;

/// <summary>Get a single channel the caller can access (member, or the global channel). 404 otherwise.</summary>
public sealed record GetChannelByIdQuery(Guid ChannelId) : IQuery<ChannelDto>;

/// <summary>List joinable office-scoped named channels the caller is NOT yet a member of.</summary>
public sealed record DiscoverChannelsQuery : IQuery<IReadOnlyList<ChannelDto>>;

/// <summary>Rename/re-topic a channel. Caller must be a channel admin.</summary>
public sealed record UpdateChannelCommand(Guid ChannelId, string Name, string? Topic = null) : ICommand<ChannelDto>;

/// <summary>Archive (soft-delete) a channel. Caller must be a channel admin.</summary>
public sealed record ArchiveChannelCommand(Guid ChannelId) : ICommand<Unit>;

/// <summary>Restore a previously archived channel. Caller must be a channel admin.</summary>
public sealed record RestoreChannelCommand(Guid ChannelId) : ICommand<ChannelDto>;

/// <summary>Add members to a channel. Caller must be a channel admin. Not valid for direct messages.</summary>
public sealed record AddChannelMembersCommand(Guid ChannelId, IReadOnlyList<string> UserIds) : ICommand<ChannelDto>;

/// <summary>Remove a member from a channel. Caller must be a channel admin.</summary>
public sealed record RemoveChannelMemberCommand(Guid ChannelId, string UserId) : ICommand<Unit>;

/// <summary>Mark the channel read up to <paramref name="MessageId"/> for the caller.</summary>
public sealed record MarkChannelReadCommand(Guid ChannelId, Guid MessageId) : ICommand<Unit>;
