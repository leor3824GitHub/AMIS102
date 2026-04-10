using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// An individual physical unit of semi-expendable property (COA Circular 2022-004).
/// Each property is assigned a unique PropertyNo by the Supply Division and tracked
/// through its full lifecycle (on-hand → issued → returned/transferred/disposed).
/// </summary>
public sealed class SemiExpendableProperty : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>
    /// Unique control number assigned by the Supply Division.
    /// Suggested format: AM-YYYY-NNNNN (e.g., AM-2024-00001).
    /// </summary>
    public string PropertyNo { get; private set; } = default!;

    /// <summary>Reference to the item catalog entry.</summary>
    public Guid SemiExpendableItemId { get; private set; }

    /// <summary>Navigation property.</summary>
    public SemiExpendableItem SemiExpendableItem { get; private set; } = default!;

    /// <summary>Manufacturer or supplier serial number, if any.</summary>
    public string? SerialNo { get; private set; }

    /// <summary>Date the item was acquired / received from the supplier.</summary>
    public DateOnly AcquisitionDate { get; private set; }

    /// <summary>Unit acquisition cost used for ICS and SPLC valuation.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Fund cluster per the Unified Accounts Code Structure (UACS).
    /// Inherits from the item catalog but can be overridden per-unit.
    /// </summary>
    public string? FundCluster { get; private set; }

    /// <summary>Current lifecycle status.</summary>
    public PropertyStatus Status { get; private set; } = PropertyStatus.OnHand;

    /// <summary>
    /// Employee ID of the current accountable officer / end-user.
    /// Populated when Status = Issued or Transferred.
    /// References MasterData.EmployeeProfile — stored as a plain FK (no cross-module navigation).
    /// </summary>
    public Guid? CurrentCustodianId { get; private set; }

    public string? Remarks { get; private set; }

    /// <summary>
    /// FK to the SMRR line item that created this property unit, if received via SMRR.
    /// Null when registered manually.
    /// </summary>
    public Guid? SMRRItemId { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static SemiExpendableProperty Create(
        string propertyNo,
        Guid semiExpendableItemId,
        string? serialNo,
        DateOnly acquisitionDate,
        decimal unitCost,
        string? fundCluster,
        string? remarks,
        Guid? smrrItemId = null)
    {
        return new SemiExpendableProperty
        {
            Id = Guid.NewGuid(),
            PropertyNo = propertyNo,
            SemiExpendableItemId = semiExpendableItemId,
            SerialNo = serialNo,
            AcquisitionDate = acquisitionDate,
            UnitCost = unitCost,
            FundCluster = fundCluster,
            Status = PropertyStatus.OnHand,
            Remarks = remarks,
            SMRRItemId = smrrItemId,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateDetails(
        string? serialNo,
        DateOnly acquisitionDate,
        decimal unitCost,
        string? fundCluster,
        string? remarks)
    {
        SerialNo = serialNo;
        AcquisitionDate = acquisitionDate;
        UnitCost = unitCost;
        FundCluster = fundCluster;
        Remarks = remarks;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetStatus(PropertyStatus status, Guid? custodianId = null)
    {
        Status = status;
        CurrentCustodianId = custodianId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
