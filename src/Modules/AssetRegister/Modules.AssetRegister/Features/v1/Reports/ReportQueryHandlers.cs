using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.AssetRegister.Data;
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

        // RegSPI (COA Annex A.4) is the SE registry — sourced from ICS accountabilities only. Cancel()
        // leaves line statuses Active, so document Status must be filtered here too: Active + Renewed,
        // never Cancelled/PendingAcceptance. (See RSPI handler note on Renewed double-counting.)
        var accountabilitiesQuery = db.PropertyAccountabilities
            .AsNoTracking()
            .Include(a => a.Lines)
            .Where(a => a.AccountabilityType == AccountabilityType.SE_ICS)
            .Where(a => a.Status == AccountabilityStatus.Active || a.Status == AccountabilityStatus.Renewed)
            .Where(a => a.IssuedOn <= asOfDate);

        if (query.CustodianId is not null)
            accountabilitiesQuery = accountabilitiesQuery.Where(a => a.ReceivedBy.EmployeeId == query.CustodianId);

        if (!string.IsNullOrWhiteSpace(query.FundCluster))
            accountabilitiesQuery = accountabilitiesQuery.Where(a => a.FundCluster == query.FundCluster);

        var accountabilities = await accountabilitiesQuery
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Flatten active lines. FundCluster comes from the accountability; the SE classification
        // (PropertyClass) is frozen on the line snapshot. Lines issued before classification snapshotting
        // carry null and are back-filled from the master AssetRegistry below.
        var flat = accountabilities
            .SelectMany(a => a.Lines
                .Where(l => l.LineStatus == AccountabilityLineStatus.Active)
                .Where(l => query.AssetType is null || l.Snapshot.AssetType == query.AssetType)
                .Select(l => new FlatRegSpiRow(
                    a.FundCluster,
                    l.Snapshot.PropertyClass,
                    new RegSpiRowDto(
                        a.Id,
                        a.DocumentNo,
                        a.IssuedOn,
                        a.ReceivedBy.EmployeeId,
                        a.ReceivedBy.PrintedName,
                        a.ReceivedBy.Designation ?? string.Empty,
                        l.Id,
                        l.AssetRegistryId,
                        l.Snapshot.PropertyNo,
                        l.Snapshot.Description,
                        l.Snapshot.AssetType,
                        l.Snapshot.Unit,
                        l.Snapshot.UnitCost,
                        l.IssuedQty,
                        l.Snapshot.UnitCost * l.IssuedQty,
                        l.SnapshotResponsibilityCenterCode))))
            .ToList();

        // Legacy fallback: resolve PropertyClass from the master AssetRegistry for snapshots that predate
        // classification snapshotting (null code). One batched lookup keyed by AssetRegistryId.
        var missingIds = flat
            .Where(f => string.IsNullOrWhiteSpace(f.PropertyClass))
            .Select(f => f.Row.AssetRegistryId)
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

            foreach (var f in flat)
                if (string.IsNullOrWhiteSpace(f.PropertyClass) && byId.TryGetValue(f.Row.AssetRegistryId, out var pc))
                    f.PropertyClass = pc;
        }

        // Optional classification filter — applied after fallback so legacy rows are filterable too.
        if (!string.IsNullOrWhiteSpace(query.PropertyClass))
            flat = flat
                .Where(f => string.Equals(f.PropertyClass, query.PropertyClass, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var nameByCode = await ResolveClassificationNamesAsync(cancellationToken).ConfigureAwait(false);

        // Group Fund Cluster → SE classification, matching the COA Annex A.4 per-sheet scoping.
        var groups = flat
            .GroupBy(f => f.FundCluster)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(fcGroup =>
            {
                var classes = fcGroup
                    .GroupBy(f => f.PropertyClass ?? string.Empty)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(clsGroup =>
                    {
                        var code = string.IsNullOrWhiteSpace(clsGroup.Key) ? null : clsGroup.Key;
                        var groupRows = clsGroup
                            .Select(f => f.Row)
                            .OrderBy(r => r.CustodianName)
                            .ThenBy(r => r.DocumentNo)
                            .ThenBy(r => r.PropertyNo)
                            .ToList();
                        return new RegSpiClassificationGroupDto(
                            code,
                            ResolveName(code, nameByCode),
                            groupRows,
                            groupRows.Count,
                            groupRows.Sum(r => r.Amount));
                    })
                    .ToList();

                return new RegSpiFundClusterGroupDto(
                    fcGroup.Key,
                    classes,
                    classes.Sum(c => c.TotalItems),
                    classes.Sum(c => c.TotalAmount));
            })
            .ToList();

        return new RegSpiReportDto(
            asOfDate,
            query.CustodianId,
            query.FundCluster,
            query.PropertyClass,
            groups,
            groups.Sum(g => g.TotalItems),
            groups.Sum(g => g.TotalAmount));
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

    private sealed class FlatRegSpiRow(string fundCluster, string? propertyClass, RegSpiRowDto row)
    {
        public string FundCluster { get; } = fundCluster;
        public string? PropertyClass { get; set; } = propertyClass;
        public RegSpiRowDto Row { get; } = row;
    }
}

public sealed class GetRegSpiFundClustersQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetRegSpiFundClustersQuery, IReadOnlyList<string>>
{
    public async ValueTask<IReadOnlyList<string>> Handle(GetRegSpiFundClustersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Only fund clusters that can actually appear on a RegSPI — same source set as the report itself
        // (SE-ICS accountabilities that are Active or Renewed). Keeps the filter dropdown free of stale values.
        var clusters = await db.PropertyAccountabilities
            .AsNoTracking()
            .Where(a => a.AccountabilityType == AccountabilityType.SE_ICS)
            .Where(a => a.Status == AccountabilityStatus.Active || a.Status == AccountabilityStatus.Renewed)
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
