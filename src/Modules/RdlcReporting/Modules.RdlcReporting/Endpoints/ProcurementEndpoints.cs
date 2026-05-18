using AMIS.Modules.RdlcReporting.Features.v1.PurchaseRequests.PrintPurchaseRequest;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.RdlcReporting.Endpoints;

/// <summary>
/// RDLC endpoints for the Procurement area. One extension method per area keeps
/// <c>RdlcReportingModule.MapEndpoints</c> a flat registry as the number of reports grows.
/// Add a new report here as a one-liner: <c>group.MapGroup("...").MapXxx();</c>
/// </summary>
internal static class ProcurementEndpoints
{
    public static IEndpointRouteBuilder MapProcurementRdlcReports(this IEndpointRouteBuilder moduleGroup)
    {
        var procurement = moduleGroup.MapGroup("/procurement");

        procurement.MapGroup("/purchase-requests").MapPurchaseRequestRdlcReports();
        // procurement.MapGroup("/purchase-orders").MapPurchaseOrderRdlcReports();

        return moduleGroup;
    }

    private static IEndpointRouteBuilder MapPurchaseRequestRdlcReports(this IEndpointRouteBuilder group)
    {
        PrintPurchaseRequestEndpoint.Map(group);
        return group;
    }
}
