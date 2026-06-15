using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.GetReceivingReport;

public sealed class GetReceivingReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetReceivingReportQuery, ReceivingReportDto?>
{
    public async ValueTask<ReceivingReportDto?> Handle(GetReceivingReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var report = await db.ReceivingReports
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);
        return report is null ? null : ReceivingMapper.ToDto(report);
    }
}

