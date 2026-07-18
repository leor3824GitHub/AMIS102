using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetRegister.Domain.Transfers;

/// <summary>
/// One asset on an inter-agency transfer offer — a snapshot of the sending agency's book values at the
/// moment the PPEIR was posted. Deliberately minimal: only what the receiving agency needs to book the
/// asset onto its own registry. Never carries the sender's asset ids, custodians, or catalog rows.
/// </summary>
public sealed class AssetTransferOfferLine : IHasTenant
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public Guid OfferId { get; private set; }
    public int ItemNo { get; private set; }

    /// <summary>The sending agency's property number. The receiver issues its own and keeps this for reconciliation.</summary>
    public string SourcePropertyNo { get; private set; } = default!;

    public string Description { get; private set; } = default!;
    public string? SerialNo { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public decimal UnitCost { get; private set; }

    /// <summary>Acquisition date on the sending agency's books — the receiver continues this timeline.</summary>
    public DateOnly OriginalAcquisitionDate { get; private set; }

    /// <summary>Accumulated depreciation booked by the sending agency, as of <see cref="DepreciationCurrentThrough"/>.</summary>
    public decimal AccumulatedDepreciation { get; private set; }

    /// <summary>
    /// Last period the sender's <see cref="AccumulatedDepreciation"/> covers. Travels with the amount so the
    /// receiver can seed its own depreciation cursor instead of replaying the schedule from acquisition.
    /// </summary>
    public DateOnly? DepreciationCurrentThrough { get; private set; }

    public decimal NetBookValue { get; private set; }

    /// <summary>UACS object code from the sender's catalog — a hint for the receiver's catalog mapping.</summary>
    public string? CatalogUacsCode { get; private set; }

    private AssetTransferOfferLine() { }

    internal static AssetTransferOfferLine Create(
        string tenantId,
        Guid offerId,
        int itemNo,
        string sourcePropertyNo,
        string description,
        string? serialNo,
        string? brand,
        string? model,
        decimal unitCost,
        DateOnly originalAcquisitionDate,
        decimal accumulatedDepreciation,
        DateOnly? depreciationCurrentThrough,
        decimal netBookValue,
        string? catalogUacsCode)
    {
        if (string.IsNullOrWhiteSpace(sourcePropertyNo))
            throw new InvalidOperationException("SourcePropertyNo is required on a transfer offer line.");
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Description is required on a transfer offer line.");
        if (unitCost <= 0m)
            throw new InvalidOperationException("Transfer offer line unit cost must be greater than zero.");
        if (accumulatedDepreciation < 0m)
            throw new InvalidOperationException("Transfer offer line accumulated depreciation cannot be negative.");
        if (accumulatedDepreciation > unitCost)
            throw new InvalidOperationException("Transfer offer line accumulated depreciation cannot exceed its unit cost.");

        return new AssetTransferOfferLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OfferId = offerId,
            ItemNo = itemNo,
            SourcePropertyNo = sourcePropertyNo.Trim().ToUpperInvariant(),
            Description = description.Trim(),
            SerialNo = string.IsNullOrWhiteSpace(serialNo) ? null : serialNo.Trim(),
            Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim(),
            Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim(),
            UnitCost = unitCost,
            OriginalAcquisitionDate = originalAcquisitionDate,
            AccumulatedDepreciation = accumulatedDepreciation,
            DepreciationCurrentThrough = depreciationCurrentThrough,
            NetBookValue = netBookValue,
            CatalogUacsCode = string.IsNullOrWhiteSpace(catalogUacsCode) ? null : catalogUacsCode.Trim()
        };
    }
}
