using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

namespace AMIS.Modules.AssetRegister.Domain.Unserviceable;

public sealed class UnserviceablePropertyItem : IHasTenant
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public Guid ReportId { get; private set; }
    public Guid AssetRegistryId { get; private set; }
    public AssetSnapshot Snapshot { get; private set; } = default!;
    public DateOnly SnapshotDateAcquired { get; private set; }
    public decimal SnapshotAcquisitionCost { get; private set; }
    public decimal SnapshotAccumulatedDepreciation { get; private set; }
    public decimal SnapshotAccumulatedImpairmentLosses { get; private set; }
    public decimal SnapshotCarryingAmount =>
        SnapshotAcquisitionCost - SnapshotAccumulatedDepreciation - SnapshotAccumulatedImpairmentLosses;
    public string? Remarks { get; private set; }

    public DisposalMethod? DisposalMethod { get; private set; }
    public string? DisposalOtherSpecify { get; private set; }
    public decimal? AppraisedValue { get; private set; }
    public DateOnly? DisposalRecordedOn { get; private set; }

    public string? SaleORNo { get; private set; }
    public decimal? SaleAmount { get; private set; }

    private UnserviceablePropertyItem() { }

    internal static UnserviceablePropertyItem Create(
        string tenantId,
        Guid reportId,
        Guid assetRegistryId,
        AssetSnapshot snapshot,
        DateOnly dateAcquired,
        decimal acquisitionCost,
        decimal accumulatedDepreciation,
        decimal accumulatedImpairmentLosses,
        string? remarks) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportId = reportId,
            AssetRegistryId = assetRegistryId,
            Snapshot = snapshot,
            SnapshotDateAcquired = dateAcquired,
            SnapshotAcquisitionCost = acquisitionCost,
            SnapshotAccumulatedDepreciation = accumulatedDepreciation,
            SnapshotAccumulatedImpairmentLosses = accumulatedImpairmentLosses,
            Remarks = remarks
        };

    internal void RecordInspectionDecision(
        DisposalMethod method,
        string? otherSpecify,
        decimal? appraisedValue)
    {
        if (method == Contracts.v1.DisposalMethod.Other && string.IsNullOrWhiteSpace(otherSpecify))
            throw new InvalidOperationException("DisposalOtherSpecify is required when DisposalMethod is Other.");

        DisposalMethod = method;
        DisposalOtherSpecify = otherSpecify;
        AppraisedValue = appraisedValue;
    }

    internal void RecordDisposal(DateOnly disposalRecordedOn, string? saleORNo, decimal? saleAmount)
    {
        if (DisposalMethod is null)
            throw new InvalidOperationException("Cannot record disposal before an inspection decision is captured.");
        if (DisposalMethod == Contracts.v1.DisposalMethod.Sale && (string.IsNullOrWhiteSpace(saleORNo) || saleAmount is null or <= 0))
            throw new InvalidOperationException("Sale disposal requires SaleORNo and a positive SaleAmount.");

        DisposalRecordedOn = disposalRecordedOn;
        SaleORNo = saleORNo;
        SaleAmount = saleAmount;
    }
}

