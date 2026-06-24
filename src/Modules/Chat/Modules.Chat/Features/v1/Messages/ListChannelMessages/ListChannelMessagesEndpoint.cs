using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Messages.ListChannelMessages;

internal static class ListChannelMessagesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/channels/{channelId:guid}/messages", ListChannelMessages)
            .WithName("Chat_ListChannelMessages")
            .WithSummary("List a channel's messages (keyset paginated, newest first)")
            .Produces<MessagePageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> ListChannelMessages(
        Guid channelId,
        Guid? before,
        int? take,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListChannelMessagesQuery(channelId, before, take), cancellationToken);
        return TypedResults.Ok(result);
    }
}
