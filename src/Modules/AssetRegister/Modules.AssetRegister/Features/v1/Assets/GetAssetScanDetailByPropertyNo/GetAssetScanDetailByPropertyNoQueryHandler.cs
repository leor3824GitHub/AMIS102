using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetAssetScanDetailByPropertyNo;

public sealed class GetAssetScanDetailByPropertyNoQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetAssetScanDetailByPropertyNoQuery, AssetScanDetailDto?>
{
    public async ValueTask<AssetScanDetailDto?> Handle(GetAssetScanDetailByPropertyNoQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalized = query.PropertyNo?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized) || !PropertyNumber.TryParse(normalized, out var pn))
            return null;

        var asset = await db.AssetRegistries
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PropertyNo == pn, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
            return null;

        // Current location name (own master data).
        string? locationName = null;
        if (asset.CurrentLocationId is Guid locationId)
        {
            locationName = await db.Locations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => l.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        // Document type/number and accountable officer come from the asset's current accountability.
        // PrintedName/Designation are denormalized on the accountability (EmployeeRef), so no
        // cross-module employee lookup is needed.
        AccountabilityType? documentType = null;
        string? documentNo = null;
        Guid? officerId = null;
        string? officerName = null;
        string? officerDesignation = null;
        if (asset.CurrentAccountabilityId is Guid accountabilityId)
        {
            var accountability = await db.PropertyAccountabilities
                .AsNoTracking()
                .Where(a => a.Id == accountabilityId)
                .Select(a => new
                {
                    a.AccountabilityType,
                    a.DocumentNo,
                    a.ReceivedBy.EmployeeId,
                    a.ReceivedBy.PrintedName,
                    a.ReceivedBy.Designation,
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (accountability is not null)
            {
                documentType = accountability.AccountabilityType;
                documentNo = accountability.DocumentNo;
                officerId = accountability.EmployeeId;
                officerName = accountability.PrintedName;
                officerDesignation = accountability.Designation;
            }
        }

        return new AssetScanDetailDto(
            asset.Id,
            asset.PropertyNo.Value,
            asset.AssetType,
            asset.Description,
            asset.SerialNo,
            asset.Brand,
            asset.Model,
            asset.Unit,
            asset.AcquisitionDate,
            asset.UnitCost,
            asset.LifecycleState,
            asset.CurrentCondition,
            asset.CurrentLocationId,
            locationName,
            asset.CurrentAccountabilityId,
            documentType,
            documentNo,
            officerId,
            officerName,
            officerDesignation);
    }
}
