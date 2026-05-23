using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetRegister.Domain.Receiving;

public sealed class ReceivingReportItem : IHasTenant
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public Guid ReportId { get; private set; }
    public Guid CatalogItemId { get; private set; }

    /// <summary>IAR document reference (PPERR) or supplier delivery note (SMRR). Optional.</summary>
    public string? Reference { get; private set; }

    /// <summary>Property number assigned to the materialized AssetRegistry row.</summary>
    public string PropertyNo { get; private set; } = default!;

    public string Description { get; private set; } = default!;
    public DateOnly AcquisitionDate { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal Amount => Quantity * UnitCost;
    public string? SerialNo { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }

    /// <summary>Snapshot of the Accountant-assigned UACS Object Code from the source PR/PO/IAR line.</summary>
    public string? UacsObjectCode { get; private set; }

    // ── Ad-hoc / external-source fields (donations, inter-agency transfers, etc.) ─────
    // Populated only when the line did NOT come from an internal Accepted IAR (i.e., SourceIARId on
    // the request was null). The receiving agency books its own PropertyNo and AcquisitionDate, but
    // preserves the original-source metadata for the audit trail and depreciation continuity.

    /// <summary>Name of the originating office or agency (e.g., "NFA Region V" or "Department of Agriculture").</summary>
    public string? SourceAgencyName { get; private set; }

    /// <summary>The source agency's PropertyNo on their books — kept for reconciliation only.</summary>
    public string? SourcePropertyNo { get; private set; }

    /// <summary>Free-text reference to the source document (e.g., "PPEIR-2026-0123 dated 2026-04-15", deed-of-donation no, PTR no).</summary>
    public string? SourceDocumentRef { get; private set; }

    /// <summary>
    /// Original acquisition date on the source agency's books. Used by AssetRegistry for depreciation
    /// continuity per COA GAM — receiving agency carries the original timeline, not the receipt date.
    /// </summary>
    public DateOnly? OriginalAcquisitionDate { get; private set; }

    private ReceivingReportItem() { }

    internal static ReceivingReportItem Create(
        string tenantId,
        Guid reportId,
        Guid catalogItemId,
        string? reference,
        string propertyNo,
        string description,
        DateOnly acquisitionDate,
        int quantity,
        decimal unitCost,
        string? serialNo,
        string? brand,
        string? model,
        string? uacsObjectCode = null,
        string? sourceAgencyName = null,
        string? sourcePropertyNo = null,
        string? sourceDocumentRef = null,
        DateOnly? originalAcquisitionDate = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Item description is required.");
        if (string.IsNullOrWhiteSpace(propertyNo))
            throw new InvalidOperationException("Property number is required.");
        if (quantity <= 0)
            throw new InvalidOperationException("Item quantity must be greater than zero.");
        if (unitCost <= 0)
            throw new InvalidOperationException("Item unit cost must be greater than zero.");

        return new ReceivingReportItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportId = reportId,
            CatalogItemId = catalogItemId,
            Reference = reference,
            PropertyNo = propertyNo,
            Description = description,
            AcquisitionDate = acquisitionDate,
            Quantity = quantity,
            UnitCost = unitCost,
            SerialNo = serialNo,
            Brand = brand,
            Model = model,
            UacsObjectCode = string.IsNullOrWhiteSpace(uacsObjectCode) ? null : uacsObjectCode.Trim(),
            SourceAgencyName = string.IsNullOrWhiteSpace(sourceAgencyName) ? null : sourceAgencyName.Trim(),
            SourcePropertyNo = string.IsNullOrWhiteSpace(sourcePropertyNo) ? null : sourcePropertyNo.Trim(),
            SourceDocumentRef = string.IsNullOrWhiteSpace(sourceDocumentRef) ? null : sourceDocumentRef.Trim(),
            OriginalAcquisitionDate = originalAcquisitionDate
        };
    }
}

