using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Reports.GetPropertyCard;

/// <summary>
/// Builds an asset's Property Card on demand by projecting the source documents that reference it
/// (receiving, accountability, issuance, unserviceable, incident). No stored ledger — the documents
/// are the source of truth, so the card never drifts and there is no write-side projection to maintain.
/// </summary>
public sealed class GetPropertyCardQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetPropertyCardQuery, PropertyCardDto?>
{
    public async ValueTask<PropertyCardDto?> Handle(GetPropertyCardQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalized = query.PropertyNo?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized) || !PropertyNumber.TryParse(normalized, out var pn))
            return null;

        var asset = await db.AssetRegistries
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PropertyNo == pn, cancellationToken).ConfigureAwait(false);
        if (asset is null)
            return null;

        var assetId = asset.Id;
        var propertyNo = asset.PropertyNo.Value;
        var rows = new List<PropertyCardRowDto>();

        // Acquisition — receiving report (PPERR/SMRR).
        var receiving = await db.ReceivingReports
            .AsNoTracking().Include(r => r.Items.Where(i => i.PropertyNo == propertyNo))
            .Where(r => r.Items.Any(i => i.PropertyNo == propertyNo))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var r in receiving)
        {
            var item = r.Items.First(i => i.PropertyNo == propertyNo);
            rows.Add(new PropertyCardRowDto(
                r.Date, AssetMovementType.Acquired, MovementSource.Receiving,
                r.ReportNo, r.Id, r.ReceivedFrom, item.UnitCost,
                $"{r.DocumentKind} · {r.ReceiptType}"));
        }

        // Issue / return — accountability (PAR/ICS).
        var accountabilities = await db.PropertyAccountabilities
            .AsNoTracking().Include(a => a.Lines.Where(l => l.AssetRegistryId == assetId))
            .Where(a => a.Lines.Any(l => l.AssetRegistryId == assetId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var a in accountabilities)
        {
            var line = a.Lines.First(l => l.AssetRegistryId == assetId);
            rows.Add(new PropertyCardRowDto(
                a.IssuedOn, AssetMovementType.Issued, MovementSource.Accountability,
                a.DocumentNo, a.Id, a.ReceivedBy.PrintedName, line.Snapshot.UnitCost,
                a.AccountabilityType.ToString()));

            if (line.LineStatus == AccountabilityLineStatus.Returned && line.ReturnedOn is not null)
                rows.Add(new PropertyCardRowDto(
                    line.ReturnedOn.Value, AssetMovementType.Returned, MovementSource.Accountability,
                    a.DocumentNo, a.Id, a.ReceivedBy.PrintedName, null,
                    line.ReturnedConditionAtReturn?.ToString()));
        }

        // Transfer out — issuance report (PPEIR/SMIR).
        var issuances = await db.PropertyIssuanceReports
            .AsNoTracking().Include(r => r.Lines.Where(l => l.AssetRegistryId == assetId))
            .Where(r => r.Lines.Any(l => l.AssetRegistryId == assetId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var r in issuances)
        {
            var line = r.Lines.First(l => l.AssetRegistryId == assetId);
            rows.Add(new PropertyCardRowDto(
                r.Date, AssetMovementType.TransferredOut, MovementSource.Issuance,
                r.ReportNo, r.Id, r.IssuedTo.PrintedName, line.SnapshotAmount,
                $"{r.ReportType} · {r.Nature}"));
        }

        // Unserviceable / disposal — IIRUP.
        var unserviceables = await db.UnserviceablePropertyReports
            .AsNoTracking().Include(r => r.Items.Where(i => i.AssetRegistryId == assetId))
            .Where(r => r.Items.Any(i => i.AssetRegistryId == assetId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var r in unserviceables)
        {
            var item = r.Items.First(i => i.AssetRegistryId == assetId);
            rows.Add(new PropertyCardRowDto(
                r.AsAt, AssetMovementType.Unserviceable, MovementSource.Unserviceable,
                r.ReportNo, r.Id, null, item.SnapshotCarryingAmount, item.Remarks));

            if (item.DisposalRecordedOn is not null)
                rows.Add(new PropertyCardRowDto(
                    item.DisposalRecordedOn.Value, AssetMovementType.Disposed, MovementSource.Unserviceable,
                    r.ReportNo, r.Id, null, item.SaleAmount,
                    item.DisposalMethod?.ToString()));
        }

        // Loss / recovery — incident report (RLSDDSP).
        var incidents = await db.PropertyIncidentReports
            .AsNoTracking().Include(r => r.Items.Where(i => i.AssetRegistryId == assetId))
            .Where(r => r.Items.Any(i => i.AssetRegistryId == assetId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var r in incidents)
        {
            var item = r.Items.First(i => i.AssetRegistryId == assetId);
            rows.Add(new PropertyCardRowDto(
                r.IncidentDate, AssetMovementType.Lost, MovementSource.Incident,
                r.IncidentNo, r.Id, null, item.SnapshotAcquisitionCost, r.IncidentType.ToString()));

            if (item.ItemResolution == IncidentItemResolution.Recovered && item.ResolvedOn is not null)
                rows.Add(new PropertyCardRowDto(
                    item.ResolvedOn.Value, AssetMovementType.Recovered, MovementSource.Incident,
                    r.IncidentNo, r.Id, null, null, null));
        }

        var ordered = rows
            .OrderBy(m => m.Date)
            .ThenBy(m => m.MovementType)
            .ToList();

        return new PropertyCardDto(
            asset.Id, propertyNo, asset.Description, asset.AssetType, asset.Unit,
            asset.AcquisitionDate, asset.UnitCost, asset.LifecycleState, ordered);
    }
}
