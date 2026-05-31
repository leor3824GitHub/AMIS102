using AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintDepartmentIssuance;
using AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintEmployeeIssuance;
using AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintPhysicalCount;
using AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintStockCard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Endpoints;

internal static class ExpendableEndpoints
{
    internal static IEndpointRouteBuilder MapExpendableQuestPdfReports(this IEndpointRouteBuilder group)
    {
        var expendable = group.MapGroup("expendable");

        PrintStockCardEndpoint.Map(expendable);
        PrintPhysicalCountEndpoint.Map(expendable);
        PrintEmployeeIssuanceEndpoint.Map(expendable);
        PrintDepartmentIssuanceEndpoint.Map(expendable);

        return group;
    }
}
