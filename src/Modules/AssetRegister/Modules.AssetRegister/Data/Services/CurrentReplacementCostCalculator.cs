using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Data.Services;

/// <summary>
/// Computes current replacement cost per COA Circular 2022-004 §4.19. The registry's best market
/// observation for "a similar asset" is the most recently acquired unit with the same category code
/// and description, acquired on or before the as-of date; <see cref="ReplacementCostPolicy"/> decides
/// whether that observation or the subject's own acquisition cost is the current price.
/// </summary>
internal sealed class CurrentReplacementCostCalculator(AssetRegisterDbContext db) : ICurrentReplacementCostCalculator
{
    public async Task<decimal> ComputeAsync(AssetRegistry subject, DateOnly asOf, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var categoryCode = subject.CategoryCode;
        var description = subject.Description.ToLower();
        var latestSimilar = await db.AssetRegistries.AsNoTracking()
            .Where(a => a.CategoryCode == categoryCode
                        && a.Description.ToLower() == description
                        && a.AcquisitionDate <= asOf
                        && a.UnitCost > 0)
            .OrderByDescending(a => a.AcquisitionDate)
            .ThenByDescending(a => a.CreatedOnUtc)
            .Select(a => new ReplacementCostPolicy.PriceObservation(a.UnitCost, a.AcquisitionDate))
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        return ReplacementCostPolicy.Select(subject.UnitCost, subject.AcquisitionDate, latestSimilar);
    }
}
