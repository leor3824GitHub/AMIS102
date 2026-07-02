using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
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

        // A PTR is a PPE transfer form — only a PPEIR may be rendered as one. The UI only lists PPEIRs,
        // but the endpoint accepts any issuance report id, so guard against SMIRs here.
        if (report.ReportType != IssuanceReportType.PPEIR)
        {
            throw new CustomException(
                $"Issuance report {report.ReportNo} is a {report.ReportType}; a PTR can only be generated from a PPEIR.",
                [], HttpStatusCode.BadRequest);
        }

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new PtrPdfDocument(
            report, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
