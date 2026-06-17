using AMIS.Modules.QuestPdfReporting.Features.v1.Finance.PrintDisbursementVoucher;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Endpoints;

internal static class FinanceEndpoints
{
    internal static IEndpointRouteBuilder MapFinanceQuestPdfReports(this IEndpointRouteBuilder group)
    {
        var finance = group.MapGroup("finance");

        PrintDisbursementVoucherEndpoint.Map(finance);

        return group;
    }
}
