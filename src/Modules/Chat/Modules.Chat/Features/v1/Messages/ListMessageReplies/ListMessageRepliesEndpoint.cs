using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Messages.ListMessageReplies;

internal static class ListMessageRepliesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/messages/{messageId:guid}/replies", ListMessageReplies)
            .WithName("Chat_ListMessageReplies")
            .WithSummary("List the replies of a single-level thread (oldest first)")
            .Produces<IReadOnlyList<MessageDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> ListMessageReplies(
        Guid messageId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListMessageRepliesQuery(messageId), cancellationToken);
        return TypedResults.Ok(result);
    }
}
