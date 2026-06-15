using AMIS.Modules.AssetRegister.Contracts.v1.Depreciation;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Depreciation.GetPpeLedgerCard;

public sealed class GetPpeLedgerCardQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetPpeLedgerCardQuery, PpeLedgerCardDto?>
{
    public async ValueTask<PpeLedgerCardDto?> Handle(GetPpeLedgerCardQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalized = query.PropertyNo?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized) || !PropertyNumber.TryParse(normalized, out var pn))
            return null;

        var asset = await db.AssetRegistries
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PropertyNo == pn, cancellationToken).ConfigureAwait(false);
        if (asset is null)
            return null;

        var entries = await db.DepreciationEntries
            .AsNoTracking()
            .Where(e => e.AssetRegistryId == asset.Id)
            .OrderBy(e => e.Period)
            .Select(e => new DepreciationEntryDto(
                e.Period, e.Amount, e.AccumulatedDepreciationAfter, e.CarryingAmountAfter, e.PostedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PpeLedgerCardDto(
            asset.Id,
            asset.PropertyNo.Value,
            asset.Description,
            asset.AssetType,
            asset.AcquisitionDate,
            asset.UnitCost,
            asset.ResidualValue,
            asset.EstimatedUsefulLifeYears,
            asset.MonthlyDepreciation(),
            asset.DepreciationStartDate,
            asset.DepreciatedThrough,
            asset.AccumulatedDepreciation,
            asset.AccumulatedImpairmentLosses,
            asset.CarryingAmount,
            asset.IsFullyDepreciated,
            entries);
    }
}
