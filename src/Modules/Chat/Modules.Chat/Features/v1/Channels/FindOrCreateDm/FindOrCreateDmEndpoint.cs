using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Channels.FindOrCreateDm;

internal static class FindOrCreateDmEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/channels/dm", FindOrCreateDm)
            .WithName("Chat_FindOrCreateDm")
            .WithSummary("Find or create a 1:1 direct message channel")
            .Produces<ChannelDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ChatPermissions.Channels.Create);

    private static async Task<IResult> FindOrCreateDm(
        FindOrCreateDmCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Ok(result);
    }
}
