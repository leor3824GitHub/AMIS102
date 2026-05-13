using FSH.Modules.AssetRegister.Contracts.v1.Receiving;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Receiving.GetReceivingReport;

public sealed class GetReceivingReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetReceivingReportQuery, ReceivingReportDto?>
{
    public async ValueTask<ReceivingReportDto?> Handle(GetReceivingReportQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var report = await db.ReceivingReports
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == query.Id, ct)
            .ConfigureAwait(false);
        return report is null ? null : ReceivingMapper.ToDto(report);
    }
}
