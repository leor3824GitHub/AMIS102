using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Messages.AddReaction;

internal static class AddReactionEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/messages/{messageId:guid}/reactions", AddReaction)
            .WithName("Chat_AddReaction")
            .WithSummary("Add your emoji reaction to a message (channel members)")
            .Produces<MessageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> AddReaction(
        Guid messageId,
        AddReactionRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddReactionCommand(messageId, request.Emoji), cancellationToken);
        return TypedResults.Ok(result);
    }

    /// <summary>Body for <see cref="AddReactionEndpoint"/>; the message id comes from the route.</summary>
    public sealed record AddReactionRequest(string Emoji);
}
