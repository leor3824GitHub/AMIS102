using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Messages.EditMessage;

internal static class EditMessageEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/messages/{messageId:guid}", EditMessage)
            .WithName("Chat_EditMessage")
            .WithSummary("Edit your own message")
            .Produces<MessageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Messages.EditOwn);

    private static async Task<IResult> EditMessage(
        Guid messageId,
        EditMessageRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new EditMessageCommand(messageId, request.Content), cancellationToken);
        return TypedResults.Ok(result);
    }

    /// <summary>Body for <see cref="EditMessageEndpoint"/>; the message id comes from the route.</summary>
    public sealed record EditMessageRequest(string Content);
}
