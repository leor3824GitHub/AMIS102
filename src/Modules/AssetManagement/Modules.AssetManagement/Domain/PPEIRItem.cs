using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a PPE Issuance Report (PPEIR).
/// Key values are frozen at transfer time as snapshots for historical accuracy.
/// AccumulatedDepreciation and BookValue are filled later by ASD/F.O. Accounting Unit.
/// </summary>
public sealed class PPEIRItem : BaseEntity<Guid>
{
    /// <summary>FK to the parent PPEIR.</summary>
    public Guid PPEIRId { get; private set; }

    /// <summary>The TangibleInventoryItem (PPE type) being transferred.</summary>
    public Guid TangibleInventoryItemId { get; private set; }

    /// <summary>Item number on the PPEIR form (sequential within the report).</summary>
    public int ItemNo { get; private set; }

    /// <summary>PPE property code — snapshot at transfer time.</summary>
    public string PropertyCode { get; private set; } = default!;

    /// <summary>Serial number of the equipment — snapshot at transfer time.</summary>
    public string? SerialNumber { get; private set; }

    /// <summary>Specific description of the PPE — snapshot at transfer time.</summary>
    public string PPESpecification { get; private set; } = default!;

    /// <summary>Acquisition date — snapshot at transfer time.</summary>
    public DateOnly DateAcquired { get; private set; }

    /// <summary>
    /// Total cost of the PPE — snapshot at transfer time.
    /// Filled by GSD/Lead Department and validated by ASD/F.O. Accounting Unit.
    /// </summary>
    public decimal AcquisitionCost { get; private set; }

    /// <summary>
    /// Total accumulated depreciation at date of transfer.
    /// Filled by ASD/F.O. Accounting Unit. Null until populated.
    /// </summary>
    public decimal? AccumulatedDepreciation { get; private set; }

    /// <summary>
    /// Net book value (AcquisitionCost − AccumulatedDepreciation).
    /// Filled by ASD/F.O. Accounting Unit. Null until populated.
    /// </summary>
    public decimal? BookValue { get; private set; }

    public static PPEIRItem Create(
        Guid ppeirId,
        Guid tangibleInventoryItemId,
        int itemNo,
        string propertyCode,
        string? serialNumber,
        string ppeSpecification,
        DateOnly dateAcquired,
        decimal acquisitionCost)
    {
        return new PPEIRItem
        {
            Id                      = Guid.NewGuid(),
            PPEIRId                 = ppeirId,
            TangibleInventoryItemId = tangibleInventoryItemId,
            ItemNo                  = itemNo,
            PropertyCode            = propertyCode,
            SerialNumber            = serialNumber,
            PPESpecification        = ppeSpecification,
            DateAcquired            = dateAcquired,
            AcquisitionCost         = acquisitionCost,
        };
    }

    /// <summary>
    /// Updates depreciation values — called by Accounting after the transfer is processed.
    /// </summary>
    public void SetDepreciation(decimal accumulatedDepreciation, decimal bookValue)
    {
        AccumulatedDepreciation = accumulatedDepreciation;
        BookValue = bookValue;
    }
}
