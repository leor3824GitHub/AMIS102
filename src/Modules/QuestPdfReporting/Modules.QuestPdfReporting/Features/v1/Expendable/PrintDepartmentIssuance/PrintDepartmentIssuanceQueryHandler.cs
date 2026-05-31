using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintDepartmentIssuance;

public sealed class PrintDepartmentIssuanceQueryHandler(IMediator mediator)
    : IQueryHandler<PrintDepartmentIssuanceQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintDepartmentIssuanceQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDepartmentIssuanceReportQuery
        {
            DepartmentId = query.DepartmentId,
            From         = query.From,
            To           = query.To,
            PageNumber   = 1,
            PageSize     = 10000,
        }, ct).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);
        var signatories = await mediator.Send(new GetReportSignatoriesQuery("DepartmentIssuance"), ct).ConfigureAwait(false);

        var data = result.Items.ToList();

        var departmentNames = data
            .Select(d => d.DepartmentId).Distinct()
            .ToDictionary(id => id, id => id);

        return new DepartmentIssuancePdfDocument(
            data, org, signatories, query.From, query.To, departmentNames,
            query.PaperSize, query.Orientation)
            .GeneratePdf();
    }
}
