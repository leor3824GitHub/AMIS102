using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetRegister.Contracts.v1;
using FSH.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Receiving.SearchReceivingReports;

public static class SearchReceivingReportsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName("AssetRegister_SearchReceivingReports")
            .WithSummary("Search Receiving Reports (PPERR / SMRR)")
            .Produces<PagedResponse<ReceivingReportSummaryDto>>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Receiving.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        ReceivingDocumentKind? documentKind = null,
        ReceiptType? receiptType = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new SearchReceivingReportsQuery(
            keyword, documentKind, receiptType, fromDate, toDate, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}
