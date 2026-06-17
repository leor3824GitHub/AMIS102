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

        return new DisbursementVoucherPdfDocument(
            dv, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
