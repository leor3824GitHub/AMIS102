using FSH.Modules.AssetRegister.Contracts.v1;
using FSH.Modules.AssetRegister.Contracts.v1.Reports;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Reports;

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
            .Select(l => new IssuanceReportLineDocumentDto(
                l.Id,
                l.AccountabilityId,
                l.AccountabilityLineId,
                l.AssetRegistryId,
                l.Snapshot.PropertyNo,
                l.Snapshot.Description,
                l.Snapshot.Unit,
                l.SnapshotResponsibilityCenterCode,
                l.SnapshotQuantityIssued,
                l.SnapshotUnitCost,
                l.SnapshotAmount))
            .ToList();

        return new IssuanceReportDocumentDto(
            report.Id,
            report.ReportNo,
            report.ReportType,
            report.Status,
            report.FundCluster,
            report.PeriodFromDate,
            report.PeriodToDate,
            report.PreparedBy.EmployeeId,
            report.PreparedBy.PrintedName,
            report.PreparedBy.Designation,
            report.CertifiedBy?.EmployeeId,
            report.CertifiedBy?.PrintedName,
            report.CertifiedBy?.Designation,
            report.PostedBy?.EmployeeId,
            report.PostedBy?.PrintedName,
            report.PostedBy?.Designation,
            report.PostedOn,
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

public sealed class GetRegSpiReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetRegSpiReportQuery, RegSpiReportDto>
{
    public async ValueTask<RegSpiReportDto> Handle(GetRegSpiReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var accountabilities = await db.PropertyAccountabilities
            .AsNoTracking()
            .Include(a => a.Lines)
            .Where(a => a.IssuedOn <= asOfDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rows = accountabilities
            .Where(a => query.CustodianId is null || a.ReceivedBy.EmployeeId == query.CustodianId)
            .SelectMany(a => a.Lines
                .Where(l => l.LineStatus == AccountabilityLineStatus.Active)
                .Where(l => query.AssetType is null || l.Snapshot.AssetType == query.AssetType)
                .Select(l => new RegSpiRowDto(
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
                    l.SnapshotResponsibilityCenterCode)))
            .OrderBy(r => r.CustodianName)
            .ThenBy(r => r.DocumentNo)
            .ThenBy(r => r.PropertyNo)
            .ToList();

        return new RegSpiReportDto(
            asOfDate,
            query.AssetType,
            query.CustodianId,
            rows,
            rows.Count,
            rows.Sum(r => r.Amount));
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