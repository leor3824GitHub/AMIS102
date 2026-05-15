using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetRegister.Domain.Receiving;

public sealed class ReceivingReportItem : IHasTenant
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public Guid ReportId { get; private set; }
    public Guid CatalogItemId { get; private set; }

    /// <summary>
    /// Supplier reference (SMRR) or pre-printed property code (PPERR). Optional;
    /// system-generated PropertyNumbers on the materialized AssetRegistry rows are
    /// the authoritative identifier.
    /// </summary>
    public string? Reference { get; private set; }

    public string Description { get; private set; } = default!;
    public DateOnly AcquisitionDate { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal Amount => Quantity * UnitCost;
    public string? SerialNo { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }

    private ReceivingReportItem() { }

    internal static ReceivingReportItem Create(
        string tenantId,
        Guid reportId,
        Guid catalogItemId,
        string? reference,
        string description,
        DateOnly acquisitionDate,
        int quantity,
        decimal unitCost,
        string? serialNo,
        string? brand,
        string? model)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Item description is required.");
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
            Description = description,
            AcquisitionDate = acquisitionDate,
            Quantity = quantity,
            UnitCost = unitCost,
            SerialNo = serialNo,
            Brand = brand,
            Model = model
        };
    }
}

