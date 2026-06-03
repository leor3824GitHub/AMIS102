using AMIS.Modules.FastReporting.Features.v1.InspectionAcceptanceReports.PrintInspectionAcceptanceReportFast;
using AMIS.Modules.FastReporting.Features.v1.Canvass.PrintAbstractOfCanvassFast;
using AMIS.Modules.FastReporting.Features.v1.Canvass.PrintRequestForQuotationFast;
using AMIS.Modules.FastReporting.Features.v1.PurchaseOrders.PrintPurchaseOrderFast;
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
        procurement.MapGroup("/purchase-orders").MapPurchaseOrderFastReports();
        procurement.MapGroup("/canvass-requests").MapCanvassRequestFastReports();
        procurement.MapGroup("/inspection-acceptance-reports").MapInspectionAcceptanceReportFastReports();

        return moduleGroup;
    }

    private static IEndpointRouteBuilder MapPurchaseRequestFastReports(this IEndpointRouteBuilder group)
    {
        PrintPurchaseRequestFastEndpoint.Map(group);
        return group;
    }

    private static IEndpointRouteBuilder MapPurchaseOrderFastReports(this IEndpointRouteBuilder group)
    {
        PrintPurchaseOrderFastEndpoint.Map(group);
        return group;
    }

    private static IEndpointRouteBuilder MapCanvassRequestFastReports(this IEndpointRouteBuilder group)
    {
        PrintAbstractOfCanvassFastEndpoint.Map(group);
        PrintRequestForQuotationFastEndpoint.Map(group);
        return group;
    }

    private static IEndpointRouteBuilder MapInspectionAcceptanceReportFastReports(this IEndpointRouteBuilder group)
    {
        PrintInspectionAcceptanceReportFastEndpoint.Map(group);
        return group;
    }
}
