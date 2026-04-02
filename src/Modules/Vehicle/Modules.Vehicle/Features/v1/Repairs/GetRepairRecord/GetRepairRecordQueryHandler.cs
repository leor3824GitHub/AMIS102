using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Features.v1.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.GetRepairRecord;

public sealed class GetRepairRecordQueryHandler(VehicleDbContext db)
    : IQueryHandler<GetRepairRecordQuery, RepairRecordDto?>
{
    public async ValueTask<RepairRecordDto?> Handle(GetRepairRecordQuery query, CancellationToken ct)
    {
        var record = await db.RepairRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == query.Id, ct)
            .ConfigureAwait(false);
        return record?.ToDto();
    }
}
