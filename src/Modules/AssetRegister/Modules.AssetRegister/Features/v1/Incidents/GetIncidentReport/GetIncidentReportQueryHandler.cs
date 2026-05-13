using AMIS.Modules.AssetRegister.Contracts.v1.Incidents;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Incidents.GetIncidentReport;

public sealed class GetIncidentReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetIncidentReportQuery, PropertyIncidentReportDto?>
{
    public async ValueTask<PropertyIncidentReportDto?> Handle(GetIncidentReportQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var report = await db.PropertyIncidentReports
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == query.Id, ct).ConfigureAwait(false);
        return report is null ? null : IncidentMapper.ToDto(report);
    }
}

