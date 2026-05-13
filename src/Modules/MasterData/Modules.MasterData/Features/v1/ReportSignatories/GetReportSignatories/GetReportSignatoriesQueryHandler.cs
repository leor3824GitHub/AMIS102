using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.ReportSignatories.GetReportSignatories;

public sealed class GetReportSignatoriesQueryHandler(MasterDataDbContext db)
    : IQueryHandler<GetReportSignatoriesQuery, List<ReportSignatoryDto>>
{
    public async ValueTask<List<ReportSignatoryDto>> Handle(
        GetReportSignatoriesQuery query, CancellationToken cancellationToken)
    {
        var result = db.ReportSignatories.AsQueryable();

        if (!string.IsNullOrEmpty(query.ReportType) && query.ReportType != "All")
        {
            result = result.Where(x => x.ReportType == query.ReportType);
        }

        return await result
            .OrderBy(x => x.ReportType)
            .ThenBy(x => x.SortOrder)
            .Select(x => new ReportSignatoryDto(x.Id, x.ReportType, x.SortOrder, x.Label, x.Name, x.Title, x.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

