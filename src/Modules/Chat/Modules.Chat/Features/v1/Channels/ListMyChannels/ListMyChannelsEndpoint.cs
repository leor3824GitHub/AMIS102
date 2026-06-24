using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Channels.ListMyChannels;

internal static class ListMyChannelsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/channels", ListMyChannels)
            .WithName("Chat_ListMyChannels")
            .WithSummary("List the channels the current user belongs to")
            .Produces<IReadOnlyList<ChannelDto>>(StatusCodes.Status200OK)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> ListMyChannels(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListMyChannelsQuery(), cancellationToken);
        return TypedResults.Ok(result);
    }
}
