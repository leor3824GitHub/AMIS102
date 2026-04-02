using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Features.v1.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.GetVehicle;

public sealed class GetVehicleQueryHandler(VehicleDbContext db)
    : IQueryHandler<GetVehicleQuery, VehicleDto?>
{
    public async ValueTask<VehicleDto?> Handle(GetVehicleQuery query, CancellationToken ct)
    {
        var vehicle = await db.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == query.Id, ct)
            .ConfigureAwait(false);
        return vehicle?.ToDto();
    }
}
