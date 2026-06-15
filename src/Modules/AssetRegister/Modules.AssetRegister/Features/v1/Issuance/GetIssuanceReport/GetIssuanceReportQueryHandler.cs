using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.GetIssuanceReport;

public sealed class GetIssuanceReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetIssuanceReportQuery, PropertyIssuanceReportDto?>
{
    public async ValueTask<PropertyIssuanceReportDto?> Handle(GetIssuanceReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var report = await db.PropertyIssuanceReports
            .AsNoTracking()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken).ConfigureAwait(false);
        return report is null ? null : IssuanceMapper.ToDto(report);
    }
}

