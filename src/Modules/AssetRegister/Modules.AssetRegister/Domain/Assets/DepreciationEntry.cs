using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetRegister.Domain.Assets;

/// <summary>
/// Append-only monthly depreciation posting for a PPE asset — the source rows that back the
/// PPE Ledger Card (PPELC). One row per (asset, period). Never updated or deleted; the unique
/// (TenantId, AssetRegistryId, Period) index keeps catch-up runs idempotent.
/// </summary>
public sealed class DepreciationEntry : IHasTenant
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public Guid AssetRegistryId { get; private set; }

    /// <summary>The depreciated month, stored as the first day of that month.</summary>
    public DateOnly Period { get; private set; }

    /// <summary>Amount charged this period (capped at the residual floor; may be a partial final month).</summary>
    public decimal Amount { get; private set; }

    public decimal AccumulatedDepreciationAfter { get; private set; }
    public decimal CarryingAmountAfter { get; private set; }
    public DateTimeOffset PostedOnUtc { get; private set; }

    private DepreciationEntry() { }

    public static DepreciationEntry Create(
        string tenantId,
        Guid assetRegistryId,
        DateOnly period,
        decimal amount,
        decimal accumulatedDepreciationAfter,
        decimal carryingAmountAfter) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AssetRegistryId = assetRegistryId,
            Period = period,
            Amount = amount,
            AccumulatedDepreciationAfter = accumulatedDepreciationAfter,
            CarryingAmountAfter = carryingAmountAfter,
            PostedOnUtc = DateTimeOffset.UtcNow
        };
}
