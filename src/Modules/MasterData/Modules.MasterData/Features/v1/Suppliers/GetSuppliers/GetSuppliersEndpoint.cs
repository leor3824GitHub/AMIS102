using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.GetSuppliers;

public static class GetSuppliersEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetSuppliers)
            .WithName(nameof(GetSuppliersQuery))
            .WithSummary("Get paginated list of suppliers")
            .Produces<PagedResponseOfSupplierDto>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataModuleConstants.Permissions.Suppliers.View);

    private static async Task<IResult> GetSuppliers(
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSuppliersQuery(keyword, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

