using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Channels.DiscoverChannels;

internal static class DiscoverChannelsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/channels/discover", DiscoverChannels)
            .WithName("Chat_DiscoverChannels")
            .WithSummary("List joinable office channels the caller has not yet joined")
            .Produces<IReadOnlyList<ChannelDto>>(StatusCodes.Status200OK)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> DiscoverChannels(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DiscoverChannelsQuery(), cancellationToken);
        return TypedResults.Ok(result);
    }
}
