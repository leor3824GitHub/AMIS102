using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Counting;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.ClosePhysicalCount;

/// <summary>
/// Closes a reconciled count session and trues up the registry so the active inventory balance
/// equals what was found (COA Circular 2020-006):
///   1. Found at station → materialized into the registry as a new Available asset (§6.2.7/§6.3.2.a).
///   2. Sign-off (domain enforces Reconciled + no pending recounts).
///   3. Found-on-books conditions mirrored onto the registry.
///   4. Missing + uncounted on-the-books assets dropped out of the active balance to
///      UnderInvestigation, routing them to the relief / disposition track (§6.2.8/§6.3.1.d).
/// All in a single transaction.
/// </summary>
public sealed class ClosePhysicalCountCommandHandler(AssetRegisterDbContext db, IMediator mediator)
    : ICommandHandler<ClosePhysicalCountCommand, PhysicalCountSessionDto>
{
    public async ValueTask<PhysicalCountSessionDto> Handle(ClosePhysicalCountCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var session = await db.PhysicalCountSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{cmd.SessionId}' not found.");

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        // 1. Recognize found-at-station items into the registry before sign-off so the Close
        //    invariant (no unmaterialized FoundAtStation entry) is satisfied by the system.
        await MaterializeFoundAtStationAsync(session, tenantId, ct).ConfigureAwait(false);

        // 2. Sign off. Domain enforces Reconciled status, materialization, and no pending recounts.
        var approvedBy = EmployeeRef.Create(cmd.ApprovedBy.EmployeeId, cmd.ApprovedBy.PrintedName, cmd.ApprovedBy.Designation);
        EmployeeRef? witnessedBy = cmd.WitnessedBy is null ? null
            : EmployeeRef.Create(cmd.WitnessedBy.EmployeeId, cmd.WitnessedBy.PrintedName, cmd.WitnessedBy.Designation);
        session.Close(approvedBy, witnessedBy, cmd.ClosedOn);

        // 3. Mirror per-entry condition onto found-on-books assets.
        await MirrorFoundConditionsAsync(session, ct).ConfigureAwait(false);

        // 4. Drop not-found assets (recorded Missing + in-scope Uncounted) out of the active balance.
        await FlagNotFoundAsync(session, ct).ConfigureAwait(false);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }

    private async Task MaterializeFoundAtStationAsync(PhysicalCountSession session, string tenantId, CancellationToken ct)
    {
        var pending = session.Entries
            .Where(e => e.Condition == PhysicalCountCondition.FoundAtStation && e.AssetRegistryId is null)
            .ToList();
        if (pending.Count == 0) return;

        // Each found-at-station item needs an operator-assigned PropertyNo + catalog item to be recognized.
        var incomplete = pending
            .Where(e => string.IsNullOrWhiteSpace(e.ProposedPropertyNo)
                        || e.ProposedCatalogItemId is null || e.ProposedCatalogItemId == Guid.Empty)
            .Select(e => $"'{e.SnapshotArticle}' (entry {e.Id}) needs an assigned Property No. and catalog item.")
            .ToList();
        if (incomplete.Count > 0)
            throw new CustomException(
                "Cannot close: found-at-station items must be assigned a Property No. and catalog item before they are recognized into the registry.",
                incomplete, HttpStatusCode.Conflict);

        var catalogIds = pending.Select(e => e.ProposedCatalogItemId!.Value).Distinct().ToList();
        var catalogs = await db.PropertyItemCatalogs
            .Where(c => catalogIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct).ConfigureAwait(false);

        var threshold = await mediator.Send(new GetActiveCapitalizationThresholdQuery(), ct).ConfigureAwait(false)
            ?? AssetClassificationPolicy.FallbackThreshold;

        foreach (var entry in pending)
        {
            if (!catalogs.TryGetValue(entry.ProposedCatalogItemId!.Value, out var catalog))
                throw new CustomException(
                    $"Cannot close: catalog item '{entry.ProposedCatalogItemId}' for found-at-station entry '{entry.SnapshotArticle}' was not found.",
                    (IEnumerable<string>?)null, HttpStatusCode.Conflict);
            if (!catalog.IsActive || string.IsNullOrWhiteSpace(catalog.UacsObjectCode))
                throw new CustomException(
                    $"Cannot close: catalog item '{catalog.Code}' for found-at-station entry '{entry.SnapshotArticle}' is inactive or has no UACS code.",
                    (IEnumerable<string>?)null, HttpStatusCode.Conflict);

            var unitCost = entry.ProposedUnitCost ?? entry.SnapshotUnitCost;
            if (unitCost <= 0)
                throw new CustomException(
                    $"Cannot close: found-at-station entry '{entry.SnapshotArticle}' needs a positive appraised unit cost before recognition.",
                    (IEnumerable<string>?)null, HttpStatusCode.Conflict);

            var (assetType, category) = AssetClassificationPolicy.Classify(catalog, unitCost, threshold);
            var propertyNo = PropertyNumber.Create(entry.ProposedPropertyNo!.Trim().ToUpperInvariant());

            var asset = AssetRegistry.Register(
                tenantId, catalog, assetType, category, propertyNo,
                description: entry.SnapshotArticle,
                serialNo: null, brand: null, model: null,
                fundCluster: session.FundCluster,
                acquisitionDate: entry.ProposedAcquisitionDate ?? session.AsAt,
                unitCost: unitCost,
                sourceIARId: null, sourcePurchaseOrderId: null);

            db.AssetRegistries.Add(asset);
            session.AttachReconciledAssetToEntry(entry.Id, asset.Id);
        }
    }

    private async Task MirrorFoundConditionsAsync(PhysicalCountSession session, CancellationToken ct)
    {
        var foundEntries = session.Entries
            .Where(e => e.AssetRegistryId is not null
                        && e.Condition != PhysicalCountCondition.Missing
                        && e.Condition != PhysicalCountCondition.FoundAtStation)
            .ToList();
        if (foundEntries.Count == 0) return;

        var assetIds = foundEntries.Select(e => e.AssetRegistryId!.Value).ToList();
        var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(ct).ConfigureAwait(false);
        var byId = assets.ToDictionary(a => a.Id);
        foreach (var entry in foundEntries)
        {
            if (!byId.TryGetValue(entry.AssetRegistryId!.Value, out var asset)) continue;
            var mapped = entry.Condition switch
            {
                PhysicalCountCondition.InGoodCondition => AssetCondition.InGoodCondition,
                PhysicalCountCondition.NeedingRepair => AssetCondition.NeedingRepair,
                PhysicalCountCondition.Unserviceable => AssetCondition.Unserviceable,
                _ => (AssetCondition?)null
            };
            if (mapped.HasValue) asset.UpdateCondition(mapped.Value);
        }
    }

    private async Task FlagNotFoundAsync(PhysicalCountSession session, CancellationToken ct)
    {
        // Assets recorded as Missing on this session.
        var missingIds = session.Entries
            .Where(e => e.Condition == PhysicalCountCondition.Missing && e.AssetRegistryId is not null)
            .Select(e => e.AssetRegistryId!.Value)
            .ToHashSet();

        // Any asset now linked to an entry was accounted for (found-on-books + materialized found-at-station).
        var countedIds = session.Entries
            .Where(e => e.AssetRegistryId is not null)
            .Select(e => e.AssetRegistryId!.Value)
            .ToHashSet();

        // Uncounted: in-scope, on-the-books assets never recorded — treated as missing (§6.3.1.d).
        var uncountedIds = await db.AssetRegistries
            .InCountScope(session.FundCluster, session.Scope)
            .Where(a => !countedIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync(ct).ConfigureAwait(false);

        var toFlag = missingIds.Concat(uncountedIds).Distinct().ToList();
        if (toFlag.Count == 0) return;

        var assets = await db.AssetRegistries.Where(a => toFlag.Contains(a.Id)).ToListAsync(ct).ConfigureAwait(false);
        foreach (var asset in assets)
            asset.MarkMissingFromCount(session.Id);
    }
}
