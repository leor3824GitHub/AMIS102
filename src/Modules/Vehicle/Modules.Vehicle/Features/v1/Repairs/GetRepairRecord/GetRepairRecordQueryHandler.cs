using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Features.v1.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.GetRepairRecord;

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

