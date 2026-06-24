using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Messages.RemoveReaction;

internal static class RemoveReactionEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/messages/{messageId:guid}/reactions", RemoveReaction)
            .WithName("Chat_RemoveReaction")
            .WithSummary("Remove your emoji reaction from a message (channel members)")
            .Produces<MessageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> RemoveReaction(
        Guid messageId,
        string emoji,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveReactionCommand(messageId, emoji), cancellationToken);
        return TypedResults.Ok(result);
    }
}
