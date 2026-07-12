using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.Lookups;

public static class MasterDataLookupEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/employees", SearchEmployees)
            .WithName(nameof(SearchEmployeeReferencesQuery))
            .WithSummary("Search employee references")
            .Produces<PagedResponse<EmployeeReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/employees/{id:guid}", GetEmployeeById)
            .WithName(nameof(GetEmployeeReferenceByIdQuery))
            .WithSummary("Get employee reference by id")
            .Produces<EmployeeReferenceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/employees/by-identity/{identityUserId}", GetEmployeeByIdentity)
            .WithName(nameof(GetEmployeeReferenceByIdentityUserIdQuery))
            .WithSummary("Get employee reference by identity user id")
            .Produces<EmployeeReferenceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        // Batch: resolve many employee references in one call (replaces UI per-row N+1 fetches).
        endpoints.MapPost("/employees/by-ids", GetEmployeesByIds)
            .WithName("MasterData_GetEmployeeReferencesByIds")
            .WithSummary("Get employee references for a set of ids (batch)")
            .Produces<IReadOnlyDictionary<Guid, EmployeeReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        // Unpaged "all active" lookups for bounded dropdowns — never truncated by the page-size clamp.
        endpoints.MapGet("/departments/all", GetAllDepartments)
            .WithName("MasterData_ListAllDepartments")
            .WithSummary("List all active departments (unpaged lookup)")
            .Produces<IReadOnlyList<DepartmentReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/offices/all", GetAllOffices)
            .WithName("MasterData_ListAllOffices")
            .WithSummary("List all active offices (unpaged lookup)")
            .Produces<IReadOnlyList<OfficeReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/positions/all", GetAllPositions)
            .WithName("MasterData_ListAllPositions")
            .WithSummary("List all active positions (unpaged lookup)")
            .Produces<IReadOnlyList<PositionReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/unit-of-measures/all", GetAllUnitOfMeasures)
            .WithName("MasterData_ListAllUnitOfMeasures")
            .WithSummary("List all active unit of measures (unpaged lookup)")
            .Produces<IReadOnlyList<UnitOfMeasureReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/categories/all", GetAllCategories)
            .WithName("MasterData_ListAllCategories")
            .WithSummary("List all active categories (unpaged lookup)")
            .Produces<IReadOnlyList<AMIS.Modules.MasterData.Contracts.v1.Categories.CategoryDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/suppliers/all", GetAllSuppliers)
            .WithName("MasterData_ListAllSuppliers")
            .WithSummary("List all active suppliers (unpaged lookup)")
            .Produces<IReadOnlyList<AMIS.Modules.MasterData.Contracts.v1.Suppliers.SupplierDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/offices", ListOffices)
            .WithName(nameof(ListOfficeReferencesQuery))
            .WithSummary("Search office references with pagination")
            .Produces<PagedResponse<OfficeReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/departments", ListDepartments)
            .WithName(nameof(ListDepartmentReferencesQuery))
            .WithSummary("Search department references with pagination")
            .Produces<PagedResponse<DepartmentReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/positions", ListPositions)
            .WithName(nameof(ListPositionReferencesQuery))
            .WithSummary("Search position references with pagination")
            .Produces<PagedResponse<PositionReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);

        endpoints.MapGet("/unit-of-measures", ListUnitOfMeasures)
            .WithName(nameof(ListUnitOfMeasureReferencesQuery))
            .WithSummary("Search unit of measure references with pagination")
            .Produces<PagedResponse<UnitOfMeasureReferenceDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataPermissions.Lookup.View);
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

    private static async Task<IResult> GetEmployeesByIds(
        [FromBody] IReadOnlyCollection<Guid> ids,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEmployeeReferencesByIdsQuery(ids ?? []), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetAllDepartments(IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new ListAllDepartmentsQuery(), cancellationToken));

    private static async Task<IResult> GetAllOffices(IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new ListAllOfficesQuery(), cancellationToken));

    private static async Task<IResult> GetAllPositions(IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new ListAllPositionsQuery(), cancellationToken));

    private static async Task<IResult> GetAllUnitOfMeasures(IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new ListAllUnitOfMeasuresQuery(), cancellationToken));

    private static async Task<IResult> GetAllCategories(IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new ListAllCategoriesQuery(), cancellationToken));

    private static async Task<IResult> GetAllSuppliers(IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new ListAllSuppliersQuery(), cancellationToken));

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



