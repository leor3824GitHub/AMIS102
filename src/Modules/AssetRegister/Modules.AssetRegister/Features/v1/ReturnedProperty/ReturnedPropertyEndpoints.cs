using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.ReturnedProperty;

internal static class ReturnedPropertyEndpoints
{
    public static void MapReturnedPropertyEndpoints(IEndpointRouteBuilder group)
    {
        group.MapPost("/", async (CreateReturnedPropertyReceiptCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return TypedResults.Created($"api/v1/asset-register/returned-property/{result.Id}", result);
        })
            .WithModuleName<CreateReturnedPropertyReceiptCommand>()
            .WithSummary("Create a Receipt for Returned Property (RRSP / RRP)")
            .RequirePermission(AssetRegisterModuleConstants.Permissions.ReturnedProperty.Create);

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetReturnedPropertyReceiptQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
            .WithModuleName<GetReturnedPropertyReceiptQuery>()
            .WithSummary("Get a returned-property receipt by ID")
            .RequirePermission(AssetRegisterModuleConstants.Permissions.ReturnedProperty.View);

        group.MapGet("/", async (
            [AsParameters] SearchReturnedPropertyReceiptsQuery query,
            IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
            .WithModuleName<SearchReturnedPropertyReceiptsQuery>()
            .WithSummary("Search returned-property receipts")
            .RequirePermission(AssetRegisterModuleConstants.Permissions.ReturnedProperty.View);
    }
}
