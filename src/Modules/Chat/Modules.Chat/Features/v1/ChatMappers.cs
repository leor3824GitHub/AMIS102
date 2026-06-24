using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Domain;

namespace AMIS.Modules.Chat.Features.v1;

/// <summary>Maps Chat domain entities to their contract DTOs.</summary>
internal static class ChatMappers
{
    public static ChannelDto ToDto(this ChatChannel channel) =>
        new(
            channel.Id,
            channel.Name,
            channel.Topic,
            channel.Type.ToString(),
            channel.Scope.ToString(),
            channel.LastMessageId,
            channel.LastMessageAtUtc,
            channel.CreatedOnUtc,
            channel.Members.Select(m => m.ToDto()).ToList());

    public static ChannelMemberDto ToDto(this ChannelMember member) =>
        new(member.UserId, member.Role.ToString(), member.JoinedOnUtc);

    /// <summary>
    /// Maps a message to its DTO. Deleted (tombstoned) messages return null content so the UI can render a
    /// "message deleted" placeholder. Reactions are grouped by emoji.
    /// </summary>
    public static MessageDto ToDto(this Message message) =>
        new(
            message.Id,
            message.ChannelId,
            message.SenderId,
            message.IsDeleted ? null : message.Content,
            message.ParentMessageId,
            message.ReplyCount,
            message.IsPinned,
            message.IsDeleted,
            message.CreatedOnUtc,
            message.EditedOnUtc,
            message.Mentions.Select(x => x.MentionedUserId).ToList(),
            message.Reactions
                .GroupBy(r => r.Emoji, StringComparer.Ordinal)
                .Select(g => new MessageReactionDto(g.Key, g.Count(), g.Select(r => r.UserId).ToList()))
                .ToList());
}
