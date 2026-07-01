using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPtr;

public sealed class PrintPtrQueryHandler(IMediator mediator)
    : IQueryHandler<PrintPtrQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintPtrQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // PTR source data IS the PPEIR — reuse the existing issuance report document projection.
        var report = await mediator.Send(
            new GetIssuanceReportDocumentQuery(query.ReportId), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Issuance report {query.ReportId} not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new PtrPdfDocument(
            report, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
