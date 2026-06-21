using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.BudgetDisbursement.PrintBudgetUtilizationRequest;

public sealed class PrintBudgetUtilizationRequestQueryHandler(IMediator mediator)
    : IQueryHandler<PrintBudgetUtilizationRequestQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintBudgetUtilizationRequestQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var bur = await mediator.Send(new GetBudgetUtilizationRequestByIdQuery(query.BurId), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Budget utilization record '{query.BurId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);
        var financeSettings = await mediator.Send(new GetBudgetDisbursementSettingsQuery(), cancellationToken).ConfigureAwait(false);

        // Payee / Address on the BURS band come from the obligated purchase order (the BUR itself is
        // budget-side and carries no payee data). Office is intentionally left blank.
        var po = await mediator.Send(new GetPurchaseOrderQuery(bur.PurchaseOrderId), cancellationToken).ConfigureAwait(false);

        return new BudgetUtilizationRequestPdfDocument(
            bur, org, financeSettings, po?.SupplierName, po?.SupplierAddress,
            query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
