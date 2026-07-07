using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.PropertyClasses;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Reports;

/// <summary>
/// Registry of Property, Plant and Equipment Issued (RegPPEI) — the PPE counterpart of the RegSPI
/// (COA Annex A.4) registry. Same transaction-ledger model as <see cref="GetRegSpiReportQueryHandler"/>
/// but sourced from PPE_PAR accountabilities, RRP return receipts, and PPE disposals.
/// </summary>
public sealed class GetRegPpeiReportQueryHandler(AssetRegisterDbContext db, IMediator mediator)
    : IQueryHandler<GetRegPpeiReportQuery, RegPpeiReportDto>
{
    public async ValueTask<RegPpeiReportDto> Handle(GetRegPpeiReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // RegPPEI is a transaction ledger, not a point-in-time listing: every issue, return, re-issue
        // and disposal up to the as-of date is its own registry row. Four event streams are merged
        // below, then grouped Fund Cluster × PPE classification (one sheet each) with a running balance
        // of the quantity still in the custody of end-users.

        // ── Issue / re-issue events — PPE PAR accountabilities ────────────────────────────────────
        // History is included (documents whose lines were later returned still contribute their issue
        // rows), so only Cancelled and PendingAcceptance documents are excluded. Renewal successors
        // (SupersedesAccountabilityId != null) re-document custody that already exists — they are not
        // movements and would double-count the balance.
        var documents = await db.PropertyAccountabilities
            .AsNoTracking()
            .Include(a => a.Lines)
            .Where(a => a.AccountabilityType == AccountabilityType.PPE_PAR)
            .Where(a => a.Status != AccountabilityStatus.Cancelled
                     && a.Status != AccountabilityStatus.PendingAcceptance)
            .Where(a => a.IssuedOn <= asOfDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var events = new List<LedgerEvent>();

        foreach (var a in documents.Where(a => a.SupersedesAccountabilityId == null))
        {
            if (query.CustodianId is not null && a.ReceivedBy.EmployeeId != query.CustodianId)
                continue;

            foreach (var l in a.Lines)
            {
                events.Add(new LedgerEvent
                {
                    FundCluster = a.FundCluster,
                    PropertyClass = l.Snapshot.PropertyClass,
                    Date = a.IssuedOn,
                    ReferenceNo = a.DocumentNo,
                    AssetRegistryId = l.AssetRegistryId,
                    PropertyNo = l.Snapshot.PropertyNo,
                    Description = l.Snapshot.Description,
                    EstimatedUsefulLifeYears = l.Snapshot.EstimatedUsefulLifeYears,
                    Type = RegSpiTransactionType.Issued, // re-classified to Reissued below
                    Qty = l.IssuedQty,
                    OfficerEmployeeId = a.ReceivedBy.EmployeeId,
                    OfficerName = a.ReceivedBy.PrintedName,
                    Amount = l.Snapshot.UnitCost * l.IssuedQty
                });
            }
        }

        // The second and later issues of the same asset are re-issues (the asset was returned in between).
        foreach (var assetIssues in events.GroupBy(e => e.AssetRegistryId))
        {
            foreach (var later in assetIssues
                .OrderBy(e => e.Date)
                .ThenBy(e => e.ReferenceNo, StringComparer.OrdinalIgnoreCase)
                .Skip(1))
            {
                later.Type = RegSpiTransactionType.Reissued;
            }
        }

        // With a custodian filter, returns and disposals are scoped to assets that custodian was issued.
        var issuedAssetIds = query.CustodianId is null
            ? null
            : events.Select(e => e.AssetRegistryId).ToHashSet();

        // ── Return events ─────────────────────────────────────────────────────────────────────────
        // Primary source: accepted RRP receipts (the official PPE return document — carries the receipt
        // number the form's "PAR/RRP No." column expects). Legacy returns recorded directly on the
        // accountability line (no receipt) fall back to the line's ReturnedOn with the PAR number.
        var docById = documents.ToDictionary(a => a.Id);

        var receipts = await db.ReturnedPropertyReceipts
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.ReceiptType == ReturnedPropertyReceiptType.RRP)
            .Where(r => r.Status == ReturnedPropertyReceiptStatus.Accepted)
            .Where(r => r.Date <= asOfDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var receiptedLineIds = new HashSet<Guid>();
        foreach (var r in receipts)
        {
            docById.TryGetValue(r.AccountabilityId, out var doc);

            foreach (var item in r.Items)
            {
                receiptedLineIds.Add(item.AccountabilityLineId);

                if (issuedAssetIds is not null && !issuedAssetIds.Contains(item.AssetRegistryId))
                    continue;

                events.Add(new LedgerEvent
                {
                    FundCluster = doc?.FundCluster ?? string.Empty,
                    PropertyClass = item.Snapshot.PropertyClass,
                    Date = r.Date,
                    ReferenceNo = r.ReceiptNo ?? r.AccountabilityDocumentNo,
                    AssetRegistryId = item.AssetRegistryId,
                    PropertyNo = item.Snapshot.PropertyNo,
                    Description = item.Snapshot.Description,
                    EstimatedUsefulLifeYears = item.Snapshot.EstimatedUsefulLifeYears,
                    Type = RegSpiTransactionType.Returned,
                    Qty = 1,
                    OfficerEmployeeId = r.ReturnedBy.EmployeeId,
                    OfficerName = r.ReturnedBy.PrintedName,
                    Amount = item.Snapshot.UnitCost,
                    Remarks = DescribeCondition(item.InspectedCondition)
                });
            }
        }

        foreach (var a in documents)
        {
            foreach (var l in a.Lines)
            {
                if (l.LineStatus != AccountabilityLineStatus.Returned || l.ReturnedOn is null || l.ReturnedOn > asOfDate)
                    continue;
                if (receiptedLineIds.Contains(l.Id))
                    continue;
                if (issuedAssetIds is not null && !issuedAssetIds.Contains(l.AssetRegistryId))
                    continue;

                events.Add(new LedgerEvent
                {
                    FundCluster = a.FundCluster,
                    PropertyClass = l.Snapshot.PropertyClass,
                    Date = l.ReturnedOn.Value,
                    ReferenceNo = a.DocumentNo,
                    AssetRegistryId = l.AssetRegistryId,
                    PropertyNo = l.Snapshot.PropertyNo,
                    Description = l.Snapshot.Description,
                    EstimatedUsefulLifeYears = l.Snapshot.EstimatedUsefulLifeYears,
                    Type = RegSpiTransactionType.Returned,
                    Qty = l.ReturnedQty,
                    OfficerEmployeeId = a.ReceivedBy.EmployeeId,
                    OfficerName = a.ReceivedBy.PrintedName,
                    Amount = l.Snapshot.UnitCost * l.ReturnedQty,
                    Remarks = DescribeCondition(l.ReturnedConditionAtReturn)
                });
            }
        }

        // ── Disposal events — unserviceable property items with a recorded disposal ─────────────
        var disposals = await db.UnserviceablePropertyReports
            .AsNoTracking()
            .SelectMany(r => r.Items
                .Where(i => i.DisposalRecordedOn != null && i.DisposalRecordedOn <= asOfDate)
                .Where(i => i.Snapshot.AssetType == AssetType.PPE)
                .Select(i => new
                {
                    r.ReportNo,
                    r.FundCluster,
                    i.AssetRegistryId,
                    i.Snapshot,
                    i.DisposalRecordedOn,
                    i.DisposalMethod,
                    i.DisposalOtherSpecify,
                    i.SaleORNo
                }))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Disposal rows must land on the same sheet as the asset's issue rows or the sheet balance
        // never closes — resolve the fund cluster from the asset's latest issue when available.
        var fcByAsset = events
            .Where(e => e.Type is RegSpiTransactionType.Issued or RegSpiTransactionType.Reissued)
            .GroupBy(e => e.AssetRegistryId)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Date).Last().FundCluster);

        foreach (var d in disposals.OrderBy(d => d.DisposalRecordedOn).DistinctBy(d => d.AssetRegistryId))
        {
            if (issuedAssetIds is not null && !issuedAssetIds.Contains(d.AssetRegistryId))
                continue;

            events.Add(new LedgerEvent
            {
                FundCluster = fcByAsset.GetValueOrDefault(d.AssetRegistryId) ?? d.FundCluster,
                PropertyClass = d.Snapshot.PropertyClass,
                Date = d.DisposalRecordedOn!.Value,
                ReferenceNo = d.ReportNo,
                AssetRegistryId = d.AssetRegistryId,
                PropertyNo = d.Snapshot.PropertyNo,
                Description = d.Snapshot.Description,
                EstimatedUsefulLifeYears = d.Snapshot.EstimatedUsefulLifeYears,
                Type = RegSpiTransactionType.Disposed,
                Qty = 1,
                Amount = d.Snapshot.UnitCost,
                Remarks = DescribeDisposal(d.DisposalMethod, d.DisposalOtherSpecify, d.SaleORNo)
            });
        }

        // Legacy fallback: resolve PropertyClass from the master AssetRegistry for snapshots that predate
        // classification snapshotting (null code). One batched lookup keyed by AssetRegistryId.
        var missingIds = events
            .Where(e => string.IsNullOrWhiteSpace(e.PropertyClass))
            .Select(e => e.AssetRegistryId)
            .Distinct()
            .ToList();
        if (missingIds.Count > 0)
        {
            var byId = await db.AssetRegistries
                .AsNoTracking()
                .Where(ar => missingIds.Contains(ar.Id))
                .Select(ar => new { ar.Id, ar.PropertyClass })
                .ToDictionaryAsync(x => x.Id, x => x.PropertyClass, cancellationToken)
                .ConfigureAwait(false);

            foreach (var e in events)
                if (string.IsNullOrWhiteSpace(e.PropertyClass) && byId.TryGetValue(e.AssetRegistryId, out var pc))
                    e.PropertyClass = pc;
        }

        // Keep every movement of an asset on the same sheet: canonical classification = the one frozen
        // at first issue. A return/disposal snapshotted after a reclassification would otherwise land
        // on a different sheet and skew both sheets' balances.
        var pcByAsset = events
            .Where(e => e.Type is RegSpiTransactionType.Issued or RegSpiTransactionType.Reissued)
            .GroupBy(e => e.AssetRegistryId)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Date).First().PropertyClass);
        foreach (var e in events)
            if (pcByAsset.TryGetValue(e.AssetRegistryId, out var pc))
                e.PropertyClass = pc;

        // Optional filters — applied after the fallback/canonicalization so legacy rows behave too.
        if (!string.IsNullOrWhiteSpace(query.FundCluster))
            events = events
                .Where(e => string.Equals(e.FundCluster, query.FundCluster, StringComparison.Ordinal))
                .ToList();

        if (!string.IsNullOrWhiteSpace(query.PropertyClass))
            events = events
                .Where(e => string.Equals(e.PropertyClass, query.PropertyClass, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var nameByCode = await ResolveClassificationNamesAsync(cancellationToken).ConfigureAwait(false);

        var officeByEmployee = await ResolveOfficesAsync(
            events.Where(e => e.OfficerEmployeeId is not null).Select(e => e.OfficerEmployeeId!.Value),
            cancellationToken).ConfigureAwait(false);

        // The reporting entity's own name is printed once as "Entity Name" in each sheet header, so the
        // Office/Officer cells suppress it — showing only the officer when the office is the entity itself.
        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);
        var entityName = org?.Name;

        // Group Fund Cluster → PPE classification (one sheet each) and run the balance.
        var sheetNo = 0;
        var groups = events
            .GroupBy(e => e.FundCluster)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(fcGroup =>
            {
                var sheets = fcGroup
                    .GroupBy(e => e.PropertyClass ?? string.Empty)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(clsGroup =>
                    {
                        var code = string.IsNullOrWhiteSpace(clsGroup.Key) ? null : clsGroup.Key;

                        // Chronological ledger; ties on the same asset and date resolve in enum order
                        // (Issued < Returned < Reissued < Disposed), which is the only physical sequence.
                        var ordered = clsGroup
                            .OrderBy(e => e.Date)
                            .ThenBy(e => e.PropertyNo, StringComparer.OrdinalIgnoreCase)
                            .ThenBy(e => (int)e.Type)
                            .ToList();

                        var rows = new List<RegPpeiLedgerRowDto>(ordered.Count);
                        // Assets currently with end-users and the amount they were issued at — drives
                        // both the running balance and the sheet's closing BalanceAmount.
                        var custody = new Dictionary<Guid, decimal>();
                        var balance = 0;

                        foreach (var e in ordered)
                        {
                            switch (e.Type)
                            {
                                case RegSpiTransactionType.Issued:
                                case RegSpiTransactionType.Reissued:
                                    if (custody.TryAdd(e.AssetRegistryId, e.Amount))
                                        balance += e.Qty;
                                    break;
                                case RegSpiTransactionType.Returned:
                                    if (custody.Remove(e.AssetRegistryId))
                                        balance -= e.Qty;
                                    break;
                                case RegSpiTransactionType.Disposed:
                                    // Deducts only when disposed straight from custody (condemned while
                                    // issued); an already-returned asset was deducted at its return row.
                                    if (custody.Remove(e.AssetRegistryId))
                                        balance -= e.Qty;
                                    break;
                            }

                            rows.Add(new RegPpeiLedgerRowDto(
                                e.Date,
                                e.ReferenceNo,
                                e.AssetRegistryId,
                                e.PropertyNo,
                                e.Description,
                                e.EstimatedUsefulLifeYears,
                                e.Type,
                                e.Qty,
                                FormatOfficeOfficer(e, officeByEmployee, entityName),
                                balance,
                                e.Amount,
                                e.Remarks));
                        }

                        return new RegPpeiClassificationGroupDto(
                            code,
                            ResolveName(code, nameByCode),
                            ++sheetNo,
                            rows,
                            ordered.Where(e => e.Type == RegSpiTransactionType.Issued).Sum(e => e.Qty),
                            ordered.Where(e => e.Type == RegSpiTransactionType.Returned).Sum(e => e.Qty),
                            ordered.Where(e => e.Type == RegSpiTransactionType.Reissued).Sum(e => e.Qty),
                            ordered.Where(e => e.Type == RegSpiTransactionType.Disposed).Sum(e => e.Qty),
                            balance,
                            custody.Values.Sum());
                    })
                    .ToList();

                return new RegPpeiFundClusterGroupDto(
                    fcGroup.Key,
                    sheets,
                    sheets.Sum(s => s.BalanceQty),
                    sheets.Sum(s => s.BalanceAmount));
            })
            .ToList();

        return new RegPpeiReportDto(
            asOfDate,
            query.CustodianId,
            query.FundCluster,
            query.PropertyClass,
            groups,
            groups.Sum(g => g.Classifications.Sum(c => c.Rows.Count)),
            groups.Sum(g => g.BalanceQty),
            groups.Sum(g => g.BalanceAmount));
    }

    private static string ResolveName(string? code, IReadOnlyDictionary<string, string> nameByCode)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "(Unclassified)";

        return nameByCode.TryGetValue(code, out var name) && !string.IsNullOrWhiteSpace(name) ? name : code;
    }

    // Friendly classification names are resolved live from the MasterData PropertyClass library (not frozen
    // on the snapshot): names are cosmetic and rename rarely, while the frozen code is the stable grouping key.
    private async ValueTask<IReadOnlyDictionary<string, string>> ResolveClassificationNamesAsync(CancellationToken cancellationToken)
    {
        var tree = await mediator.Send(new GetPropertyClassTreeQuery(), cancellationToken).ConfigureAwait(false);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in tree)
            map[c.Code] = c.Name;
        return map;
    }

    // Office/department is not carried on the frozen EmployeeRef, so it is resolved from MasterData at
    // query time — same pattern as the RegSPI/RSPI/RPI handlers.
    private async ValueTask<IReadOnlyDictionary<Guid, string?>> ResolveOfficesAsync(
        IEnumerable<Guid> employeeIds, CancellationToken cancellationToken)
    {
        var ids = employeeIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string?>();

        var map = await mediator
            .Send(new GetEmployeeReferencesByIdsQuery(ids), cancellationToken)
            .ConfigureAwait(false);

        return map.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value.OfficeName);
    }

    private static string? FormatOfficeOfficer(
        LedgerEvent e, IReadOnlyDictionary<Guid, string?> officeByEmployee, string? entityName)
    {
        if (e.OfficerName is null)
            return null;

        var office = e.OfficerEmployeeId is { } id ? officeByEmployee.GetValueOrDefault(id) : null;

        // Drop the office when it is the reporting entity itself — it is already printed as the sheet's
        // "Entity Name" header, so repeating it in every Office/Officer cell is redundant.
        if (string.IsNullOrWhiteSpace(office) || IsSameAsEntity(office, entityName))
            return e.OfficerName;

        return $"{office} / {e.OfficerName}";
    }

    private static bool IsSameAsEntity(string office, string? entityName) =>
        !string.IsNullOrWhiteSpace(entityName)
        && string.Equals(office.Trim(), entityName.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string? DescribeCondition(AssetCondition? condition) => condition switch
    {
        AssetCondition.InGoodCondition => "Returned in good condition",
        AssetCondition.NeedingRepair => "Returned — needing repair",
        AssetCondition.Unserviceable => "Returned — unserviceable",
        _ => null
    };

    private static string DescribeDisposal(DisposalMethod? method, string? otherSpecify, string? saleORNo) => method switch
    {
        DisposalMethod.Sale => string.IsNullOrWhiteSpace(saleORNo) ? "Disposed — sale" : $"Disposed — sale (OR {saleORNo})",
        DisposalMethod.Transfer => "Disposed — transfer",
        DisposalMethod.Destruction => "Disposed — destruction",
        DisposalMethod.Other => string.IsNullOrWhiteSpace(otherSpecify) ? "Disposed — other" : $"Disposed — {otherSpecify}",
        _ => "Disposed"
    };

    /// <summary>Mutable pre-grouping movement; <see cref="Type"/>, <see cref="FundCluster"/> and
    /// <see cref="PropertyClass"/> are refined after construction (re-issue detection, legacy
    /// classification fallback, per-asset sheet canonicalization).</summary>
    private sealed class LedgerEvent
    {
        public required string FundCluster { get; set; }
        public string? PropertyClass { get; set; }
        public required DateOnly Date { get; init; }
        public required string ReferenceNo { get; init; }
        public required Guid AssetRegistryId { get; init; }
        public required string PropertyNo { get; init; }
        public required string Description { get; init; }
        public required int EstimatedUsefulLifeYears { get; init; }
        public required RegSpiTransactionType Type { get; set; }
        public required int Qty { get; init; }
        public Guid? OfficerEmployeeId { get; init; }
        public string? OfficerName { get; init; }
        public required decimal Amount { get; init; }
        public string? Remarks { get; init; }
    }
}

public sealed class GetRegPpeiFundClustersQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetRegPpeiFundClustersQuery, IReadOnlyList<string>>
{
    public async ValueTask<IReadOnlyList<string>> Handle(GetRegPpeiFundClustersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Only fund clusters that can actually appear on a RegPPEI — same source set as the report itself
        // (all non-cancelled, accepted PPE-PAR accountabilities; the ledger includes returned history).
        var clusters = await db.PropertyAccountabilities
            .AsNoTracking()
            .Where(a => a.AccountabilityType == AccountabilityType.PPE_PAR)
            .Where(a => a.Status != AccountabilityStatus.Cancelled && a.Status != AccountabilityStatus.PendingAcceptance)
            .Where(a => a.FundCluster != null && a.FundCluster != "")
            .Select(a => a.FundCluster)
            .Distinct()
            .OrderBy(fc => fc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return clusters;
    }
}
