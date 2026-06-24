using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Channels.AddChannelMembers;

internal static class AddChannelMembersEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/channels/{channelId:guid}/members", AddChannelMembers)
            .WithName("Chat_AddChannelMembers")
            .WithSummary("Add members to a channel (channel admins only)")
            .Produces<ChannelDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.Create);

    private static async Task<IResult> AddChannelMembers(
        Guid channelId,
        AddChannelMembersRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddChannelMembersCommand(channelId, request.UserIds), cancellationToken);
        return TypedResults.Ok(result);
    }

    /// <summary>Body for <see cref="AddChannelMembersEndpoint"/>; the channel id comes from the route.</summary>
    public sealed record AddChannelMembersRequest(IReadOnlyList<string> UserIds);
}
