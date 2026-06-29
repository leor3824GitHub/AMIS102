using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.GetEnrollableVehicleAssets;

/// <summary>
/// Lists PPE motor-vehicle assets (property class LT) that are not yet enrolled as fleet vehicles.
/// The enrolled set is local knowledge, so we fetch LT PPE assets from AssetRegister via Contracts
/// then anti-join against <c>Vehicles.AssetRegistryId</c>.
/// </summary>
public sealed class GetEnrollableVehicleAssetsQueryHandler(VehicleDbContext db, IMediator mediator)
    : IQueryHandler<GetEnrollableVehicleAssetsQuery, List<EnrollableVehicleAssetDto>>
{
    public async ValueTask<List<EnrollableVehicleAssetDto>> Handle(
        GetEnrollableVehicleAssetsQuery query, CancellationToken cancellationToken)
    {
        var assets = await mediator.Send(new SearchAssetsQuery(
            Keyword: query.Keyword,
            AssetType: AssetType.PPE,
            PropertyClass: AssetClassCodes.Vehicle,
            PageNumber: 1,
            PageSize: 1000), cancellationToken).ConfigureAwait(false);

        var enrolledIds = await db.Vehicles
            .AsNoTracking()
            .Select(v => v.AssetRegistryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var enrolled = enrolledIds.ToHashSet();

        return assets.Items
            .Where(a => !enrolled.Contains(a.Id)
                && a.LifecycleState != LifecycleState.Disposed
                && a.LifecycleState != LifecycleState.TransferredOut)
            .Select(a => new EnrollableVehicleAssetDto(a.Id, a.PropertyNo, a.Description, a.UnitCost, a.AcquisitionDate))
            .ToList();
    }
}
