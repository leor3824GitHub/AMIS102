using AMIS.Modules.QuestPdfReporting.Features.v1.Procurement.PrintJobOrder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Endpoints;

internal static class ProcurementEndpoints
{
    internal static IEndpointRouteBuilder MapProcurementQuestPdfReports(this IEndpointRouteBuilder group)
    {
        var procurement = group.MapGroup("procurement");

        PrintJobOrderEndpoint.Map(procurement);

        return group;
    }
}
