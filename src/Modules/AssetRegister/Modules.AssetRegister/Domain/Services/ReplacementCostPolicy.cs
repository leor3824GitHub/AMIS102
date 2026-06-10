namespace AMIS.Modules.AssetRegister.Domain.Services;

/// <summary>
/// COA Circular 2022-004 §4.19 — current replacement cost is the current market price of a similar
/// asset as at the reporting date. The most current price the entity holds for an item is the unit
/// cost of the most recently acquired similar unit; when no similar unit was acquired after the
/// subject itself, the subject's own acquisition cost is already the latest observation.
/// </summary>
public static class ReplacementCostPolicy
{
    /// <summary>A market-price observation: what was paid for a similar unit and when.</summary>
    public sealed record PriceObservation(decimal UnitCost, DateOnly AcquisitionDate);

    public static decimal Select(decimal subjectUnitCost, DateOnly subjectAcquisitionDate, PriceObservation? latestSimilar)
    {
        if (latestSimilar is null
            || latestSimilar.UnitCost <= 0
            || latestSimilar.AcquisitionDate < subjectAcquisitionDate)
        {
            return subjectUnitCost;
        }

        return latestSimilar.UnitCost;
    }
}
