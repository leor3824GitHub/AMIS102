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
