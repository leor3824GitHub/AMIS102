using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Finance.PrintDisbursementVoucher;

public sealed class PrintDisbursementVoucherQueryHandler(IMediator mediator)
    : IQueryHandler<PrintDisbursementVoucherQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintDisbursementVoucherQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dv = await mediator.Send(new GetDisbursementVoucherByIdQuery(query.DvId), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Disbursement voucher '{query.DvId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        // The Responsibility Center code printed on the voucher is captured on the obligating BUR, not on
        // the DV itself. Resolve it from the linked BUR; if that lookup fails the form simply prints blank.
        string? responsibilityCenter = null;
        if (dv.BudgetUtilizationRecordId != Guid.Empty)
        {
            try
            {
                var bur = await mediator.Send(new GetBudgetUtilizationRecordByIdQuery(dv.BudgetUtilizationRecordId), cancellationToken).ConfigureAwait(false);
                responsibilityCenter = bur?.ResponsibilityCenter;
            }
            catch (KeyNotFoundException)
            {
                // Legacy/orphaned voucher with no resolvable BUR — leave the Responsibility Center blank.
            }
        }

        return new DisbursementVoucherPdfDocument(
            dv, org, responsibilityCenter, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
