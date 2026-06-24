using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Users;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Users.SearchChatUsers;

internal static class SearchChatUsersEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/users", SearchChatUsers)
            .WithName("Chat_SearchChatUsers")
            .WithSummary("Search mentionable users for the chat mention picker")
            .Produces<IReadOnlyList<ChatUserDto>>(StatusCodes.Status200OK)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> SearchChatUsers(
        string? q,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SearchChatUsersQuery(q), cancellationToken);
        return TypedResults.Ok(result);
    }
}
