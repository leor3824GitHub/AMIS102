using FSH.Modules.AssetRegister.Contracts.v1.Issuance;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Issuance.GetIssuanceReport;

public sealed class GetIssuanceReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetIssuanceReportQuery, PropertyIssuanceReportDto?>
{
    public async ValueTask<PropertyIssuanceReportDto?> Handle(GetIssuanceReportQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var report = await db.PropertyIssuanceReports
            .AsNoTracking()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == query.Id, ct).ConfigureAwait(false);
        return report is null ? null : IssuanceMapper.ToDto(report);
    }
}
