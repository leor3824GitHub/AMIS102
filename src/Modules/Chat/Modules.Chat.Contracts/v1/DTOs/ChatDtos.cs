namespace AMIS.Modules.Chat.Contracts.v1.DTOs;

/// <summary>A channel as returned to clients. <see cref="Type"/>/<see cref="Scope"/> are the enum names.</summary>
public sealed record ChannelDto(
    Guid Id,
    string? Name,
    string? Topic,
    string Type,
    string Scope,
    Guid? LastMessageId,
    DateTimeOffset? LastMessageAtUtc,
    DateTimeOffset CreatedOnUtc,
    IReadOnlyList<ChannelMemberDto> Members);

/// <summary>A channel membership row. Absent for the global channel (implicit membership).</summary>
public sealed record ChannelMemberDto(
    string UserId,
    string Role,
    DateTimeOffset JoinedOnUtc);

/// <summary>
/// A chat message. When <see cref="IsDeleted"/> is true the row is a tombstone and
/// <see cref="Content"/> is null — clients render a "message deleted" placeholder.
/// </summary>
public sealed record MessageDto(
    Guid Id,
    Guid ChannelId,
    string SenderId,
    string? Content,
    Guid? ParentMessageId,
    int ReplyCount,
    bool IsPinned,
    bool IsDeleted,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? EditedOnUtc,
    IReadOnlyList<string> MentionedUserIds,
    IReadOnlyList<MessageReactionDto> Reactions);

/// <summary>Reactions grouped by emoji.</summary>
public sealed record MessageReactionDto(
    string Emoji,
    int Count,
    IReadOnlyList<string> UserIds);

/// <summary>
/// A keyset (cursor) page of messages, newest first. The client passes <see cref="NextCursor"/>
/// back as the next request's "before" cursor to load older messages.
/// </summary>
public sealed record MessagePageDto(
    IReadOnlyList<MessageDto> Items,
    Guid? NextCursor,
    bool HasMore);
