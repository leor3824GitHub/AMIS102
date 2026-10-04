using AMIS.Modules.QuestPdfReporting.Features.v1.Procurement.PrintJobOrder;
using AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintApp;
using AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintPpmp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Endpoints;

internal static class ProcurementEndpoints
{
    internal static IEndpointRouteBuilder MapProcurementQuestPdfReports(this IEndpointRouteBuilder group)
    {
        var procurement = group.MapGroup("procurement");

        PrintJobOrderEndpoint.Map(procurement);

        var planning = group.MapGroup("procurement-planning");
        PrintAppEndpoint.Map(planning);
        PrintPpmpEndpoint.Map(planning);

        return group;
    }
}
