using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintAccountability;

public sealed class PrintAccountabilityQueryHandler(IMediator mediator)
    : IQueryHandler<PrintAccountabilityQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintAccountabilityQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await mediator.Send(new GetAccountabilityReportQuery(query.AccountabilityId), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability '{query.AccountabilityId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        return new AccountabilityPdfDocument(
            report, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
