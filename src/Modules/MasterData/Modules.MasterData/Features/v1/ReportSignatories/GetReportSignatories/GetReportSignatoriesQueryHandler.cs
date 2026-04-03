using FSH.Modules.MasterData.Contracts.v1.ReportSignatories;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.ReportSignatories.GetReportSignatories;

public sealed class GetReportSignatoriesQueryHandler(MasterDataDbContext db)
    : IQueryHandler<GetReportSignatoriesQuery, List<ReportSignatoryDto>>
{
    public async ValueTask<List<ReportSignatoryDto>> Handle(
        GetReportSignatoriesQuery query, CancellationToken cancellationToken)
    {
        return await db.ReportSignatories
            .Where(x => x.ReportType == query.ReportType)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ReportSignatoryDto(x.Id, x.ReportType, x.SortOrder, x.Label, x.Name, x.Title, x.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
