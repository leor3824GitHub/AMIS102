using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.PropertyClasses;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Reports;

public sealed class GetAccountabilityReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetAccountabilityReportQuery, AccountabilityReportDto?>
{
    public async ValueTask<AccountabilityReportDto?> Handle(GetAccountabilityReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var accountability = await db.PropertyAccountabilities
            .AsNoTracking()
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == query.AccountabilityId, cancellationToken)
            .ConfigureAwait(false);

        if (accountability is null)
        {
            return null;
        }

        var lines = accountability.Lines
            .Select(l => new AccountabilityReportLineDto(
                l.Id,
                l.AssetRegistryId,
                l.Snapshot.PropertyNo,
                l.Snapshot.Description,
                l.Snapshot.Unit,
                l.Snapshot.UnitCost,
                l.SnapshotItemNo,
                l.SnapshotResponsibilityCenterCode,
                l.IssuedQty,
                l.ReturnedQty,
                l.LineStatus,
                l.ReturnedOn,
                l.ReturnedConditionAtReturn))
            .ToList();

        return new AccountabilityReportDto(
            accountability.Id,
            accountability.DocumentNo,
            accountability.AccountabilityType,
            accountability.Status,
            accountability.FundCluster,
            accountability.IssuedOn,
            accountability.ExpiresOn,
            accountability.IssuedBy.EmployeeId,
            accountability.IssuedBy.PrintedName,
            accountability.IssuedBy.Designation,
            accountability.ReceivedBy.EmployeeId,
            accountability.ReceivedBy.PrintedName,
            accountability.ReceivedBy.Designation,
            lines,
            lines.Sum(l => l.IssuedQty),
            lines.Sum(l => l.ReturnedQty),
            lines.Sum(l => l.UnitCost * l.IssuedQty));
    }
}

public sealed class GetIssuanceReportDocumentQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetIssuanceReportDocumentQuery, IssuanceReportDocumentDto?>
{
    public async ValueTask<IssuanceReportDocumentDto?> Handle(GetIssuanceReportDocumentQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await db.PropertyIssuanceReports
            .AsNoTracking()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == query.ReportId, cancellationToken)
            .ConfigureAwait(false);

        if (report is null)
        {
            return null;
        }

        var lines = report.Lines
            .OrderBy(l => l.ItemNo)
            .Select(l => new IssuanceReportLineDocumentDto(
                l.Id,
                l.AssetRegistryId,
                l.ItemNo,
                l.Snapshot.PropertyNo,
                l.Snapshot.Description,
                l.Snapshot.Unit,
                l.SnapshotUnitCost,
                l.SnapshotAmount,
                l.AccumulatedDepreciation,
                l.BookValue))
            .ToList();

        return new IssuanceReportDocumentDto(
            report.Id,
            report.ReportNo,
            report.ReportType,
            report.Nature,
            report.FundCluster,
            report.Date,
            report.IssuedBy.EmployeeId,
            report.IssuedBy.PrintedName,
            report.IssuedBy.Designation,
            report.ApprovedBy.EmployeeId,
            report.ApprovedBy.PrintedName,
            report.ApprovedBy.Designation,
            report.IssuedTo.EmployeeId,
            report.IssuedTo.PrintedName,
            report.IssuedTo.Designation,
            report.IssuedToOfficeAddress,
            report.Remarks,
            lines,
            lines.Sum(l => l.Amount));
    }
}

public sealed class GetPhysicalCountReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetPhysicalCountReportQuery, PhysicalCountReportDto?>
{
    public async ValueTask<PhysicalCountReportDto?> Handle(GetPhysicalCountReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var session = await db.PhysicalCountSessions
            .AsNoTracking()
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == query.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return null;
        }

        var entries = session.Entries
            .Where(e => query.AssetType is null || e.Snapshot?.AssetType == query.AssetType.Value)
            .Select(e => new PhysicalCountReportEntryDto(
                e.Id,
                e.AssetRegistryId,
                e.Snapshot?.PropertyNo,
                e.Snapshot?.AssetType,
                e.SnapshotArticle,
                e.SnapshotUnit,
                e.SnapshotUnitCost,
                e.Condition,
                e.LocationId,
                e.ScannedOnUtc,
                e.ScannedByEmployeeId,
                e.Remarks))
            .ToList();

        return new PhysicalCountReportDto(
            session.Id,
            session.Code,
            session.Scope,
            session.Status,
            session.FundCluster,
            session.AsAt,
            session.StartedOn,
            session.ClosedOn,
            entries,
            entries.Count,
            entries.Count(e => e.Condition == PhysicalCountCondition.Missing),
            entries.Count(e => e.Condition == PhysicalCountCondition.Unserviceable),
            entries.Count(e => e.Condition == PhysicalCountCondition.FoundAtStation),
            entries.Sum(e => e.UnitCost));
    }
}

public sealed class GetRegSpiReportQueryHandler(AssetRegisterDbContext db, IMediator mediator)
    : IQueryHandler<GetRegSpiReportQuery, RegSpiReportDto>
{
    public async ValueTask<RegSpiReportDto> Handle(GetRegSpiReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // RegSPI (COA Annex A.4) is a transaction ledger, not a point-in-time listing: every issue,
        // return, re-issue and disposal up to the as-of date is its own registry row. Four event
        // streams are merged below, then grouped Fund Cluster × SE classification (one Annex A.4
        // sheet each) with a running balance of the quantity still in the custody of end-users.

        // ── Issue / re-issue events — SE ICS accountabilities ────────────────────────────────────
        // History is included (documents whose lines were later returned still contribute their issue
        // rows), so only Cancelled and PendingAcceptance documents are excluded. Renewal successors
        // (SupersedesAccountabilityId != null) re-document custody that already exists — they are not
        // movements and would double-count the balance.
        var documents = await db.PropertyAccountabilities
            .AsNoTracking()
            .Include(a => a.Lines)
            .Where(a => a.AccountabilityType == AccountabilityType.SE_ICS)
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
                if (query.AssetType is not null && l.Snapshot.AssetType != query.AssetType)
                    continue;

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
        // Primary source: accepted RRSP receipts (the official return document — carries the receipt
        // number the form's "ICS/RRSP No." column expects). Legacy returns recorded directly on the
        // accountability line (no receipt) fall back to the line's ReturnedOn with the ICS number.
        var docById = documents.ToDictionary(a => a.Id);

        var receipts = await db.ReturnedPropertyReceipts
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.ReceiptType == ReturnedPropertyReceiptType.RRSP)
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
                if (query.AssetType is not null && item.Snapshot.AssetType != query.AssetType)
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
                if (query.AssetType is not null && l.Snapshot.AssetType != query.AssetType)
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
                .Where(i => i.Snapshot.AssetType == AssetType.SE)
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
            if (query.AssetType is not null && d.Snapshot.AssetType != query.AssetType)
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
            mediator, cancellationToken).ConfigureAwait(false);

        // The reporting entity's own name is printed once as "Entity Name" in each sheet header, so the
        // Office/Officer cells suppress it — showing only the officer when the office is the entity itself.
        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);
        var entityName = org?.Name;

        // Group Fund Cluster → SE classification (one Annex A.4 sheet each) and run the balance.
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

                        var rows = new List<RegSpiLedgerRowDto>(ordered.Count);
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

                            rows.Add(new RegSpiLedgerRowDto(
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

                        return new RegSpiClassificationGroupDto(
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

                return new RegSpiFundClusterGroupDto(
                    fcGroup.Key,
                    sheets,
                    sheets.Sum(s => s.BalanceQty),
                    sheets.Sum(s => s.BalanceAmount));
            })
            .ToList();

        return new RegSpiReportDto(
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
    // query time — same pattern as the RSPI/RPI handlers.
    private static async ValueTask<IReadOnlyDictionary<Guid, string?>> ResolveOfficesAsync(
        IEnumerable<Guid> employeeIds, IMediator mediator, CancellationToken cancellationToken)
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

public sealed class GetRegSpiFundClustersQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetRegSpiFundClustersQuery, IReadOnlyList<string>>
{
    public async ValueTask<IReadOnlyList<string>> Handle(GetRegSpiFundClustersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Only fund clusters that can actually appear on a RegSPI — same source set as the report itself
        // (all non-cancelled, accepted SE-ICS accountabilities; the ledger includes returned history).
        var clusters = await db.PropertyAccountabilities
            .AsNoTracking()
            .Where(a => a.AccountabilityType == AccountabilityType.SE_ICS)
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

public sealed class GetRspiReportQueryHandler(AssetRegisterDbContext db, IMediator mediator)
    : IQueryHandler<GetRspiReportQuery, RspiReportDto>
{
    public async ValueTask<RspiReportDto> Handle(GetRspiReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 20 : query.PageSize;

        // SE property issued via ICS. Active (or, when ActiveOnly is false, Active + Renewed); returned
        // lines are dropped. NOTE: including Renewed alongside its Active successor can double-count the
        // carried-over assets — revisit if renewal volume grows.
        var baseQuery =
            from a in db.PropertyAccountabilities.AsNoTracking()
            where a.AccountabilityType == AccountabilityType.SE_ICS
            where query.ActiveOnly
                ? a.Status == AccountabilityStatus.Active
                : a.Status == AccountabilityStatus.Active || a.Status == AccountabilityStatus.Renewed
            from l in a.Lines
            where l.LineStatus == AccountabilityLineStatus.Active
            select new { a, l };

        if (query.DateFrom.HasValue)
            baseQuery = baseQuery.Where(x => x.a.IssuedOn >= query.DateFrom.Value);
        if (query.DateTo.HasValue)
            baseQuery = baseQuery.Where(x => x.a.IssuedOn <= query.DateTo.Value);
        if (query.AssetType.HasValue)
            baseQuery = baseQuery.Where(x => x.l.Snapshot.AssetType == query.AssetType.Value);

        var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        // Cast to decimal? so an empty result set yields SQL NULL → 0 instead of throwing on SUM over no rows.
        var overallAmountTotal = (await baseQuery
            .Select(x => (decimal?)(x.l.Snapshot.UnitCost * x.l.IssuedQty))
            .SumAsync(cancellationToken)
            .ConfigureAwait(false)) ?? 0m;

        // Clamp via long math — (pageNumber - 1) * pageSize overflows int for crafted query params
        // (e.g. pageNumber=3&pageSize=int.MaxValue), which would produce a negative SQL OFFSET.
        var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var pageRows = await baseQuery
            .OrderBy(x => x.a.IssuedOn).ThenBy(x => x.a.DocumentNo).ThenBy(x => x.l.Snapshot.PropertyNo)
            .Skip(skip).Take(pageSize)
            .Select(x => new
            {
                x.a.Id,
                x.a.DocumentNo,
                x.a.IssuedOn,
                x.a.Status,
                x.a.FundCluster,
                x.a.ExpiresOn,
                ReceivedById          = x.a.ReceivedBy.EmployeeId,
                ReceivedByName        = x.a.ReceivedBy.PrintedName,
                ReceivedByDesignation = x.a.ReceivedBy.Designation,
                IssuedById            = x.a.IssuedBy.EmployeeId,
                IssuedByName          = x.a.IssuedBy.PrintedName,
                IssuedByDesignation   = x.a.IssuedBy.Designation,
                x.l.AssetRegistryId,
                x.l.Snapshot.PropertyNo,
                x.l.Snapshot.Description,
                x.l.Snapshot.AssetType,
                x.l.Snapshot.Unit,
                x.l.Snapshot.UnitCost,
                x.l.IssuedQty
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var offices = await ResolveOfficesAsync(
            pageRows.Select(r => r.ReceivedById).Concat(pageRows.Select(r => r.IssuedById)),
            mediator, cancellationToken).ConfigureAwait(false);

        var items = pageRows
            .Select(r => new RspiRowDto(
                r.Id, r.DocumentNo, r.IssuedOn, r.Status, r.FundCluster, r.ExpiresOn,
                r.ReceivedById, r.ReceivedByName, r.ReceivedByDesignation, offices.GetValueOrDefault(r.ReceivedById),
                r.IssuedById, r.IssuedByName, r.IssuedByDesignation, offices.GetValueOrDefault(r.IssuedById),
                r.AssetRegistryId, r.PropertyNo, r.Description, r.AssetType, r.Unit, r.UnitCost,
                r.IssuedQty, r.UnitCost * r.IssuedQty))
            .ToList();

        return new RspiReportDto(
            query.DateFrom, query.DateTo, query.AssetType, query.ActiveOnly,
            items, pageNumber, pageSize, totalCount, overallAmountTotal);
    }

    // Office/department is not carried on the frozen AssetSnapshot/EmployeeRef, so it is resolved from
    // MasterData at query time. Printed names/designations stay sourced from the snapshot so historical
    // reports survive employee renames.
    private static async ValueTask<IReadOnlyDictionary<Guid, string?>> ResolveOfficesAsync(
        IEnumerable<Guid> employeeIds, IMediator mediator, CancellationToken cancellationToken)
    {
        var ids = employeeIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string?>();

        var map = await mediator
            .Send(new GetEmployeeReferencesByIdsQuery(ids), cancellationToken)
            .ConfigureAwait(false);

        return map.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value.OfficeName);
    }
}

public sealed class GetRpiReportQueryHandler(AssetRegisterDbContext db, IMediator mediator)
    : IQueryHandler<GetRpiReportQuery, RpiReportDto>
{
    public async ValueTask<RpiReportDto> Handle(GetRpiReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 20 : query.PageSize;

        // PPE issued via PAR. Active + Renewed; returned lines dropped. (See RSPI handler note on renewal.)
        var baseQuery =
            from a in db.PropertyAccountabilities.AsNoTracking()
            where a.AccountabilityType == AccountabilityType.PPE_PAR
            where a.Status == AccountabilityStatus.Active || a.Status == AccountabilityStatus.Renewed
            from l in a.Lines
            where l.LineStatus == AccountabilityLineStatus.Active
            select new { a, l };

        if (query.DateFrom.HasValue)
            baseQuery = baseQuery.Where(x => x.a.IssuedOn >= query.DateFrom.Value);
        if (query.DateTo.HasValue)
            baseQuery = baseQuery.Where(x => x.a.IssuedOn <= query.DateTo.Value);

        var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var overallAmountTotal = (await baseQuery
            .Select(x => (decimal?)(x.l.Snapshot.UnitCost * x.l.IssuedQty))
            .SumAsync(cancellationToken)
            .ConfigureAwait(false)) ?? 0m;

        // Clamp via long math — see RSPI handler note on int overflow for crafted query params.
        var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var pageRows = await baseQuery
            .OrderBy(x => x.a.IssuedOn).ThenBy(x => x.a.DocumentNo).ThenBy(x => x.l.Snapshot.PropertyNo)
            .Skip(skip).Take(pageSize)
            .Select(x => new
            {
                x.a.Id,
                x.a.DocumentNo,
                x.a.IssuedOn,
                x.a.Status,
                x.a.FundCluster,
                x.a.ExpiresOn,
                ReceivedById          = x.a.ReceivedBy.EmployeeId,
                ReceivedByName        = x.a.ReceivedBy.PrintedName,
                ReceivedByDesignation = x.a.ReceivedBy.Designation,
                IssuedById            = x.a.IssuedBy.EmployeeId,
                IssuedByName          = x.a.IssuedBy.PrintedName,
                IssuedByDesignation   = x.a.IssuedBy.Designation,
                x.l.AssetRegistryId,
                x.l.Snapshot.PropertyNo,
                x.l.Snapshot.Description,
                x.l.Snapshot.Unit,
                x.l.Snapshot.UnitCost,
                x.l.IssuedQty,
                x.l.Snapshot.EstimatedUsefulLifeYears,
                x.l.Snapshot.AcquisitionDate
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var offices = await ResolveOfficesAsync(
            pageRows.Select(r => r.ReceivedById).Concat(pageRows.Select(r => r.IssuedById)),
            mediator, cancellationToken).ConfigureAwait(false);

        var items = pageRows
            .Select(r => new RpiRowDto(
                r.Id, r.DocumentNo, r.IssuedOn, r.Status, r.FundCluster, r.ExpiresOn,
                r.ReceivedById, r.ReceivedByName, r.ReceivedByDesignation, offices.GetValueOrDefault(r.ReceivedById),
                r.IssuedById, r.IssuedByName, r.IssuedByDesignation, offices.GetValueOrDefault(r.IssuedById),
                r.AssetRegistryId, r.PropertyNo, r.Description, r.Unit, r.IssuedQty, r.UnitCost,
                r.UnitCost * r.IssuedQty, r.EstimatedUsefulLifeYears, r.AcquisitionDate))
            .ToList();

        return new RpiReportDto(
            query.DateFrom, query.DateTo, items, pageNumber, pageSize, totalCount, overallAmountTotal);
    }

    private static async ValueTask<IReadOnlyDictionary<Guid, string?>> ResolveOfficesAsync(
        IEnumerable<Guid> employeeIds, IMediator mediator, CancellationToken cancellationToken)
    {
        var ids = employeeIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string?>();

        var map = await mediator
            .Send(new GetEmployeeReferencesByIdsQuery(ids), cancellationToken)
            .ConfigureAwait(false);

        return map.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value.OfficeName);
    }
}

public sealed class GetIncidentReportDocumentQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetIncidentReportDocumentQuery, IncidentReportDocumentDto?>
{
    public async ValueTask<IncidentReportDocumentDto?> Handle(GetIncidentReportDocumentQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await db.PropertyIncidentReports
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == query.IncidentReportId, cancellationToken)
            .ConfigureAwait(false);

        if (report is null)
        {
            return null;
        }

        var items = report.Items
            .Select(i => new IncidentReportItemDocumentDto(
                i.Id,
                i.AssetRegistryId,
                i.Snapshot.PropertyNo,
                i.Snapshot.Description,
                i.Snapshot.AssetType,
                i.SnapshotAcquisitionCost,
                i.SnapshotCurrentReplacementCost,
                i.ItemResolution,
                i.ResolvedOn))
            .ToList();

        return new IncidentReportDocumentDto(
            report.Id,
            report.IncidentNo,
            report.IncidentType,
            report.Status,
            report.IncidentDate,
            report.FundCluster,
            report.DepartmentOffice,
            report.Circumstances,
            report.AccountableOfficer.EmployeeId,
            report.AccountableOfficer.PrintedName,
            report.AccountableOfficerDesignation ?? string.Empty,
            items,
            items.Sum(i => i.AcquisitionCost),
            items.Sum(i => i.CurrentReplacementCost));
    }
}

public sealed class GetUnserviceableReportDocumentQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetUnserviceableReportDocumentQuery, UnserviceableReportDocumentDto?>
{
    public async ValueTask<UnserviceableReportDocumentDto?> Handle(GetUnserviceableReportDocumentQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await db.UnserviceablePropertyReports
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == query.ReportId, cancellationToken)
            .ConfigureAwait(false);

        if (report is null)
        {
            return null;
        }

        var items = report.Items
            .Select(i => new UnserviceableReportItemDocumentDto(
                i.Id,
                i.AssetRegistryId,
                i.Snapshot.PropertyNo,
                i.Snapshot.Description,
                i.Snapshot.AssetType,
                i.SnapshotDateAcquired,
                i.SnapshotAcquisitionCost,
                i.SnapshotAccumulatedDepreciation,
                i.SnapshotAccumulatedImpairmentLosses,
                i.SnapshotCarryingAmount,
                i.DisposalMethod,
                i.DisposalRecordedOn,
                i.SaleORNo,
                i.SaleAmount,
                i.Remarks))
            .ToList();

        return new UnserviceableReportDocumentDto(
            report.Id,
            report.ReportNo,
            report.ReportType,
            report.Status,
            report.AsAt,
            report.FundCluster,
            report.Station,
            report.AccountableOfficer.EmployeeId,
            report.AccountableOfficer.PrintedName,
            report.AccountableOfficer.Designation ?? string.Empty,
            items,
            items.Sum(i => i.CarryingAmount));
    }
}
