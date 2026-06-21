using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.BudgetDisbursement.PrintDisbursementVoucher;

public sealed class PrintDisbursementVoucherQueryHandler(IMediator mediator)
    : IQueryHandler<PrintDisbursementVoucherQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintDisbursementVoucherQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dv = await mediator.Send(new GetDisbursementVoucherByIdQuery(query.DvId), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Disbursement voucher '{query.DvId}' not found.");

        var orgTask      = mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).AsTask();
        var settingsTask = mediator.Send(new GetBudgetDisbursementSettingsQuery(), cancellationToken).AsTask();
        await Task.WhenAll(orgTask, settingsTask).ConfigureAwait(false);

        var org             = await orgTask;
        var financeSettings = await settingsTask;

        // The Responsibility Center code printed on the voucher is captured on the obligating BUR, not on
        // the DV itself. Resolve it from the linked BUR; if that lookup fails the form simply prints blank.
        string? responsibilityCenter = null;
        if (dv.BudgetUtilizationRequestId != Guid.Empty)
        {
            try
            {
                var bur = await mediator.Send(new GetBudgetUtilizationRequestByIdQuery(dv.BudgetUtilizationRequestId), cancellationToken).ConfigureAwait(false);
                responsibilityCenter = bur?.ResponsibilityCenter;
            }
            catch (KeyNotFoundException)
            {
                // Legacy/orphaned voucher with no resolvable BUR — leave the Responsibility Center blank.
            }
        }

        return new DisbursementVoucherPdfDocument(
            dv, org, financeSettings, responsibilityCenter, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
