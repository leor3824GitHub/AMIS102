using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.ReconcilePhysicalCount;

public sealed class ReconcilePhysicalCountCommandHandler(
    AssetRegisterDbContext db,
    IPropertyNumberGenerator propertyNumbers,
    ILogger<ReconcilePhysicalCountCommandHandler> logger)
    : ICommandHandler<ReconcilePhysicalCountCommand, PhysicalCountSessionDto>
{
    private const decimal HighValuedThreshold = 50_000m;

    public async ValueTask<PhysicalCountSessionDto> Handle(ReconcilePhysicalCountCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var session = await db.PhysicalCountSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{cmd.SessionId}' not found.");

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var catalogs = await db.PropertyItemCatalogs.Where(c => c.IsActive).ToListAsync(ct).ConfigureAwait(false);

        // Materialize FoundAtStation entries into new AssetRegistry rows BEFORE Reconcile()
        // flips the status. Each FoundAtStation entry's ProposedPropertyClass picks a catalog row.
        foreach (var entry in session.Entries.Where(e => e.Condition == PhysicalCountCondition.FoundAtStation && e.AssetRegistryId is null))
        {
            var catalog = ResolveCatalog(catalogs, entry.ProposedPropertyClass);
            if (catalog is null)
            {
                logger.LogWarning(
                    "[{Tenant}] Session {SessionId} entry {EntryId}: cannot materialize FoundAtStation — no matching catalog (ProposedPropertyClass='{Hint}'). Add the catalog row and re-reconcile.",
                    tenantId, session.Id, entry.Id, entry.ProposedPropertyClass);
                continue;
            }

            var (assetType, category) = Classify(catalog, entry.ProposedUnitCost ?? entry.SnapshotUnitCost);
            var acquisitionDate = entry.ProposedAcquisitionDate ?? session.AsAt;
            var propertyNo = await propertyNumbers.NextAsync(
                assetType,
                subMajorAccount: "01",
                generalLedgerAccount: "01",
                locationCode: "00",
                acquisitionDate,
                ct).ConfigureAwait(false);

            var asset = AssetRegistry.Register(
                tenantId,
                catalog,
                assetType,
                category,
                propertyNo,
                description: entry.SnapshotArticle,
                serialNo: null,
                brand: null,
                model: null,
                fundCluster: session.FundCluster,
                acquisitionDate: acquisitionDate,
                unitCost: entry.ProposedUnitCost ?? entry.SnapshotUnitCost,
                sourceIARId: null,
                sourcePurchaseOrderId: null);

            db.AssetRegistries.Add(asset);
            asset.RecordFoundAtStation(session.Id, entry.Id);
            session.AttachReconciledAssetToEntry(entry.Id, asset.Id);
        }

        // Domain reconcile raises AssetReportedMissingFromCountEvent — handled separately.
        session.Reconcile();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }

    private static PropertyItemCatalog? ResolveCatalog(IReadOnlyList<PropertyItemCatalog> catalogs, string? propertyClassHint)
    {
        if (string.IsNullOrWhiteSpace(propertyClassHint)) return null;
        return catalogs.FirstOrDefault(c =>
            string.Equals(c.DefaultPropertyClass, propertyClassHint, StringComparison.OrdinalIgnoreCase));
    }

    private static (AssetType, AssetCategory) Classify(PropertyItemCatalog catalog, decimal unitCost)
    {
        if (catalog.DefaultPropertyClass.Contains("PPE", StringComparison.OrdinalIgnoreCase))
            return (AssetType.PPE, AssetCategory.PPE);

        return unitCost >= HighValuedThreshold
            ? (AssetType.SE, AssetCategory.HighValuedSemi)
            : (AssetType.SE, AssetCategory.LowValuedSemi);
    }
}

