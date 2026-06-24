using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Messages.UnpinMessage;

internal static class UnpinMessageEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/messages/{messageId:guid}/unpin", UnpinMessage)
            .WithName("Chat_UnpinMessage")
            .WithSummary("Unpin a message (channel members)")
            .Produces<MessageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> UnpinMessage(
        Guid messageId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnpinMessageCommand(messageId), cancellationToken);
        return TypedResults.Ok(result);
    }
}
