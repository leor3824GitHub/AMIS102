using AMIS.Modules.QuestPdfReporting.Features.v1.BudgetDisbursement.PrintBudgetUtilizationRequest;
using AMIS.Modules.QuestPdfReporting.Features.v1.BudgetDisbursement.PrintDisbursementVoucher;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Endpoints;

internal static class BudgetDisbursementEndpoints
{
    internal static IEndpointRouteBuilder MapBudgetDisbursementQuestPdfReports(this IEndpointRouteBuilder group)
    {
        var budgetDisbursement = group.MapGroup("budgetdisbursement");

        PrintDisbursementVoucherEndpoint.Map(budgetDisbursement);
        PrintBudgetUtilizationRequestEndpoint.Map(budgetDisbursement);

        return group;
    }
}
