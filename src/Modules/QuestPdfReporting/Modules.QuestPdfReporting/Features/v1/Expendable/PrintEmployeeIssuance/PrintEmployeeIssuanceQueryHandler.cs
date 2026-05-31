using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintEmployeeIssuance;

public sealed class PrintEmployeeIssuanceQueryHandler(IMediator mediator)
    : IQueryHandler<PrintEmployeeIssuanceQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintEmployeeIssuanceQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEmployeeIssuanceHistoryQuery
        {
            EmployeeId = query.EmployeeId,
            From       = query.From,
            To         = query.To,
            PageNumber = 1,
            PageSize   = 10000,
        }, ct).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        var records = result.Items.ToList();

        var employeeNames = records
            .Select(r => r.EmployeeId).Distinct()
            .ToDictionary(id => id, id => id);
        var departmentNames = records
            .Select(r => r.DepartmentId).Distinct()
            .ToDictionary(id => id, id => id);

        return new EmployeeIssuancePdfDocument(
            records, org, query.From, query.To, employeeNames, departmentNames,
            query.PaperSize, query.Orientation)
            .GeneratePdf();
    }
}
