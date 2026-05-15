using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

namespace AMIS.Modules.AssetRegister.Domain.Counting;

public sealed class PhysicalCountEntry : IHasTenant
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public Guid SessionId { get; private set; }
    public Guid? AssetRegistryId { get; private set; }
    public AssetSnapshot? Snapshot { get; private set; }
    public string SnapshotArticle { get; private set; } = default!;
    public string SnapshotUnit { get; private set; } = default!;
    public decimal SnapshotUnitCost { get; private set; }
    public PhysicalCountCondition Condition { get; private set; }
    public DateTimeOffset? ScannedOnUtc { get; private set; }
    public string? PhotoPath { get; private set; }
    public Guid? ScannedByEmployeeId { get; private set; }
    public Guid LocationId { get; private set; }
    public string? Remarks { get; private set; }

    // FoundAtStation-only proposed metadata, used at reconciliation.
    public string? ProposedPropertyClass { get; private set; }
    public string? ProposedCategoryCode { get; private set; }
    public DateOnly? ProposedAcquisitionDate { get; private set; }
    public decimal? ProposedUnitCost { get; private set; }

    private PhysicalCountEntry() { }

    internal static PhysicalCountEntry CreateForKnownAsset(
        string tenantId,
        Guid sessionId,
        Guid assetRegistryId,
        AssetSnapshot snapshot,
        string article,
        string unit,
        decimal unitCost,
        PhysicalCountCondition condition,
        Guid locationId,
        DateTimeOffset? scannedOnUtc,
        Guid? scannedByEmployeeId,
        string? photoPath,
        string? remarks)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (condition is PhysicalCountCondition.FoundAtStation)
            throw new InvalidOperationException("Use AddFoundAtStationEntry for FoundAtStation rows.");

        return new PhysicalCountEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SessionId = sessionId,
            AssetRegistryId = assetRegistryId,
            Snapshot = snapshot,
            SnapshotArticle = article,
            SnapshotUnit = unit,
            SnapshotUnitCost = unitCost,
            Condition = condition,
            LocationId = locationId,
            ScannedOnUtc = scannedOnUtc,
            ScannedByEmployeeId = scannedByEmployeeId,
            PhotoPath = photoPath,
            Remarks = remarks
        };
    }

    internal static PhysicalCountEntry CreateFoundAtStation(
        string tenantId,
        Guid sessionId,
        string article,
        string unit,
        decimal unitCost,
        Guid locationId,
        string? proposedPropertyClass,
        string? proposedCategoryCode,
        DateOnly? proposedAcquisitionDate,
        decimal? proposedUnitCost,
        Guid? scannedByEmployeeId,
        string? remarks) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SessionId = sessionId,
            AssetRegistryId = null,
            Snapshot = null,
            SnapshotArticle = article,
            SnapshotUnit = unit,
            SnapshotUnitCost = unitCost,
            Condition = PhysicalCountCondition.FoundAtStation,
            LocationId = locationId,
            ProposedPropertyClass = proposedPropertyClass,
            ProposedCategoryCode = proposedCategoryCode,
            ProposedAcquisitionDate = proposedAcquisitionDate,
            ProposedUnitCost = proposedUnitCost,
            ScannedOnUtc = DateTimeOffset.UtcNow,
            ScannedByEmployeeId = scannedByEmployeeId,
            Remarks = remarks
        };

    internal void AttachReconciledAsset(Guid assetRegistryId)
    {
        if (Condition != PhysicalCountCondition.FoundAtStation)
            throw new InvalidOperationException("Only FoundAtStation entries get a reconciled asset id.");
        AssetRegistryId = assetRegistryId;
    }
}

