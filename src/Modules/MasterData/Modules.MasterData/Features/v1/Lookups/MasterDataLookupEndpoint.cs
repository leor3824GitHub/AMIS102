using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.Lookups;

public static class MasterDataLookupEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/employees", SearchEmployees)
            .WithName(nameof(SearchEmployeeReferencesQuery))
            .WithSummary("Search employee references")
            .Produces<PagedResponse<EmployeeReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataModuleConstants.Permissions.Lookup.View);

        endpoints.MapGet("/employees/{id:guid}", GetEmployeeById)
            .WithName(nameof(GetEmployeeReferenceByIdQuery))
            .WithSummary("Get employee reference by id")
            .Produces<EmployeeReferenceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.Lookup.View);

        endpoints.MapGet("/employees/by-identity/{identityUserId}", GetEmployeeByIdentity)
            .WithName(nameof(GetEmployeeReferenceByIdentityUserIdQuery))
            .WithSummary("Get employee reference by identity user id")
            .Produces<EmployeeReferenceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.Lookup.View);

        endpoints.MapGet("/offices", ListOffices)
            .WithName(nameof(ListOfficeReferencesQuery))
            .WithSummary("Search office references with pagination")
            .Produces<PagedResponse<OfficeReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataModuleConstants.Permissions.Lookup.View);

        endpoints.MapGet("/departments", ListDepartments)
            .WithName(nameof(ListDepartmentReferencesQuery))
            .WithSummary("Search department references with pagination")
            .Produces<PagedResponse<DepartmentReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataModuleConstants.Permissions.Lookup.View);

        endpoints.MapGet("/positions", ListPositions)
            .WithName(nameof(ListPositionReferencesQuery))
            .WithSummary("Search position references with pagination")
            .Produces<PagedResponse<PositionReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataModuleConstants.Permissions.Lookup.View);

        endpoints.MapGet("/unit-of-measures", ListUnitOfMeasures)
            .WithName(nameof(ListUnitOfMeasureReferencesQuery))
            .WithSummary("Search unit of measure references with pagination")
            .Produces<PagedResponse<UnitOfMeasureReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataModuleConstants.Permissions.Lookup.View);
    }

    private static async Task<IResult> SearchEmployees(
        [AsParameters] SearchEmployeeReferencesQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetEmployeeById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEmployeeReferenceByIdQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> GetEmployeeByIdentity(
        string identityUserId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEmployeeReferenceByIdentityUserIdQuery(identityUserId), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> ListOffices(
        [AsParameters] ListOfficeReferencesQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ListDepartments(
        [AsParameters] ListDepartmentReferencesQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ListPositions(
        [AsParameters] ListPositionReferencesQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ListUnitOfMeasures(
        [AsParameters] ListUnitOfMeasureReferencesQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}



