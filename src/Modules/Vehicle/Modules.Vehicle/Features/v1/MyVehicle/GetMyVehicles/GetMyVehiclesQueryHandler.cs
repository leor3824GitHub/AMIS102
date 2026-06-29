using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Features.v1.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.MyVehicle.GetMyVehicles;

/// <summary>
/// The current user's vehicles: intersect the accepted-PAR asset set (AssetRegister) with locally
/// enrolled vehicles by <c>AssetRegistryId</c>.
/// </summary>
public sealed class GetMyVehiclesQueryHandler(VehicleDbContext db, IMediator mediator)
    : IQueryHandler<GetMyVehiclesQuery, List<VehicleDto>>
{
    public async ValueTask<List<VehicleDto>> Handle(GetMyVehiclesQuery query, CancellationToken cancellationToken)
    {
        var assetIds = await mediator.Send(new GetMyAccountableAssetIdsQuery(AssetType.PPE), cancellationToken).ConfigureAwait(false);
        if (assetIds.Count == 0)
            return [];

        var idList = assetIds.ToList();
        var vehicles = await db.Vehicles
            .AsNoTracking()
            .Where(v => idList.Contains(v.AssetRegistryId))
            .OrderBy(v => v.PlateNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return vehicles.Select(v => v.ToDto()).ToList();
    }
}
