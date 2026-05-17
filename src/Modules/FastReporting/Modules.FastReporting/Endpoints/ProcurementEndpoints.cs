using AMIS.Modules.FastReporting.Features.v1.PurchaseRequests.PrintPurchaseRequestFast;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Endpoints;

/// <summary>
/// FastReport endpoints for the Procurement area. One extension method per area keeps
/// <c>FastReportingModule.MapEndpoints</c> a flat registry as the number of reports grows.
/// Add a new report here as a one-liner: <c>group.MapGroup("...").MapXxx();</c>
/// </summary>
internal static class ProcurementEndpoints
{
    public static IEndpointRouteBuilder MapProcurementFastReports(this IEndpointRouteBuilder moduleGroup)
    {
        var procurement = moduleGroup.MapGroup("/procurement");

        procurement.MapGroup("/purchase-requests").MapPurchaseRequestFastReports();
        // procurement.MapGroup("/purchase-orders").MapPurchaseOrderFastReports();

        return moduleGroup;
    }

    private static IEndpointRouteBuilder MapPurchaseRequestFastReports(this IEndpointRouteBuilder group)
    {
        PrintPurchaseRequestFastEndpoint.Map(group);
        return group;
    }
}
