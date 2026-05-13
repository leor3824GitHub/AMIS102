using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.Offices.CreateOffice;

public static class CreateOfficeEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreateOffice)
            .WithName(nameof(CreateOfficeCommand))
            .WithSummary("Create office")
            .Produces<OfficeReferenceDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(MasterDataModuleConstants.Permissions.Offices.Create);

    private static async Task<IResult> CreateOffice(
        CreateOfficeCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/master-data/offices/{result.Id}", result);
    }
}

