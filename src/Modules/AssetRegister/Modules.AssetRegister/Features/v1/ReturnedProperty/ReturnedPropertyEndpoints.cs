using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

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
            .WithSummary("Request a return of property (RRSP / RRP) — creates a Pending request")
            .RequirePermission(AssetRegisterPermissions.ReturnedProperty.Create);

        group.MapPost("/{id:guid}/inspect", async (Guid id, InspectReturnedPropertyReceiptCommand body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(body with { Id = id }, ct);
            return TypedResults.Ok(result);
        })
            .WithModuleName<InspectReturnedPropertyReceiptCommand>()
            .WithSummary("Inspector records each returned item's condition — moves the request to Inspected")
            .RequirePermission(AssetRegisterPermissions.ReturnedProperty.Inspect);

        group.MapPost("/{id:guid}/reassign-inspector", async (Guid id, ReassignReturnedPropertyInspectorCommand body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(body with { Id = id }, ct);
            return TypedResults.Ok(result);
        })
            .WithModuleName<ReassignReturnedPropertyInspectorCommand>()
            .WithSummary("Reassign the nominated inspector while the return request is still Pending")
            .RequirePermission(AssetRegisterPermissions.ReturnedProperty.Create);

        group.MapPost("/{id:guid}/accept", async (Guid id, AcceptReturnedPropertyReceiptCommand body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(body with { Id = id }, ct);
            return TypedResults.Ok(result);
        })
            .WithModuleName<AcceptReturnedPropertyReceiptCommand>()
            .WithSummary("Custodian accepts a pending return — returns assets to Available and assigns the receipt number")
            .RequirePermission(AssetRegisterPermissions.ReturnedProperty.Accept);

        group.MapPost("/{id:guid}/reject", async (Guid id, RejectReturnedPropertyReceiptCommand body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(body with { Id = id }, ct);
            return TypedResults.Ok(result);
        })
            .WithModuleName<RejectReturnedPropertyReceiptCommand>()
            .WithSummary("Custodian rejects a pending return request")
            .RequirePermission(AssetRegisterPermissions.ReturnedProperty.Accept);

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelReturnedPropertyReceiptCommand body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(body with { Id = id }, ct);
            return TypedResults.Ok(result);
        })
            .WithModuleName<CancelReturnedPropertyReceiptCommand>()
            .WithSummary("Requester withdraws their pending return request")
            .RequirePermission(AssetRegisterPermissions.ReturnedProperty.Create);

        group.MapGet("/status-counts", async (
            [AsParameters] GetReturnedPropertyStatusCountsQuery query,
            IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
            .WithModuleName<GetReturnedPropertyStatusCountsQuery>()
            .WithSummary("Status counts for returned-property requests (for filter tabs)")
            .RequirePermission(AssetRegisterPermissions.ReturnedProperty.View);

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetReturnedPropertyReceiptQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
            .WithModuleName<GetReturnedPropertyReceiptQuery>()
            .WithSummary("Get a returned-property receipt by ID")
            .RequirePermission(AssetRegisterPermissions.ReturnedProperty.View);

        group.MapGet("/", async (
            [AsParameters] SearchReturnedPropertyReceiptsQuery query,
            IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
            .WithModuleName<SearchReturnedPropertyReceiptsQuery>()
            .WithSummary("Search returned-property receipts")
            .RequirePermission(AssetRegisterPermissions.ReturnedProperty.View);
    }
}
