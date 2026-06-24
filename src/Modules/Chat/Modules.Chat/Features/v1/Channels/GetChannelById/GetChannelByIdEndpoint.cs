using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Channels.GetChannelById;

internal static class GetChannelByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/channels/{channelId:guid}", GetChannelById)
            .WithName("Chat_GetChannelById")
            .WithSummary("Get a single channel the caller can access")
            .Produces<ChannelDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> GetChannelById(
        Guid channelId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetChannelByIdQuery(channelId), cancellationToken);
        return TypedResults.Ok(result);
    }
}
