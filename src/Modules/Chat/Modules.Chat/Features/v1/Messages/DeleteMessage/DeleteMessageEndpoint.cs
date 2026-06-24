using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Messages.DeleteMessage;

internal static class DeleteMessageEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/messages/{messageId:guid}", DeleteMessage)
            .WithName("Chat_DeleteMessage")
            .WithSummary("Delete (tombstone) your own message")
            .Produces<MessageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Messages.DeleteOwn);

    private static async Task<IResult> DeleteMessage(
        Guid messageId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteMessageCommand(messageId), cancellationToken);
        return TypedResults.Ok(result);
    }
}
