using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ExportAppXlsx;

public sealed class ExportAppXlsxQueryHandler(
    ProcurementPlanningDbContext dbContext,
    IMediator mediator) : IQueryHandler<ExportAppXlsxQuery, byte[]>
{
    // Same signatory set the printed APP uses (Master Data → Report Signatories).
    private const string SignatoryReportType = "AnnualProcurementPlan";

    public async ValueTask<byte[]> Handle(ExportAppXlsxQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var app = await AppReadProjection.BuildDtoAsync(dbContext, query.AppId, cancellationToken).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        var signatories = (await mediator.Send(
                new GetReportSignatoriesQuery(SignatoryReportType), cancellationToken).ConfigureAwait(false))
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToList();

        return AppXlsxWorkbook.Build(app, org?.Name, signatories);
    }
}
