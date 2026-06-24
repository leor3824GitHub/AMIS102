using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Internal;

/// <summary>Shared message loading + authorization for command/query handlers.</summary>
internal static class MessageAuthorization
{
    /// <summary>
    /// Loads a live (non-tombstoned) message with its mentions and reactions (tracked) from a channel the caller
    /// belongs to. Throws <see cref="NotFoundException"/> (404) if the message is missing/tombstoned or the
    /// caller is not a channel member — never revealing existence to non-members.
    /// </summary>
    public static async Task<Message> RequireLiveMemberMessageAsync(
        ChatDbContext dbContext,
        Guid messageId,
        string userId,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.Messages
            .Include(m => m.Mentions)
            .Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Message not found.");

        await ChannelAuthorization
            .RequireMemberAsync(dbContext, message.ChannelId, userId, cancellationToken)
            .ConfigureAwait(false);

        if (message.IsDeleted)
        {
            throw new NotFoundException("Message not found.");
        }

        return message;
    }
}
