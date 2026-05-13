using FSH.Modules.AssetRegister.Contracts.v1.Unserviceable;
using FSH.Modules.AssetRegister.Contracts.v1.ValueObjects;
using FSH.Modules.AssetRegister.Data;
using FSH.Modules.AssetRegister.Domain.Services;
using FSH.Modules.AssetRegister.Domain.Unserviceable;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Unserviceable;

public sealed class CreateUnserviceableReportDraftCommandHandler(
    AssetRegisterDbContext db, IUnserviceableReportNumberGenerator numbers)
    : ICommandHandler<CreateUnserviceableReportDraftCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        CreateUnserviceableReportDraftCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var reportNo = await numbers.NextAsync(cmd.ReportType, cmd.AsAt, ct).ConfigureAwait(false);
        var officer = EmployeeRef.Create(
            cmd.AccountableOfficer.EmployeeId, cmd.AccountableOfficer.PrintedName, cmd.AccountableOfficer.Designation);

        var report = UnserviceablePropertyReport.CreateDraft(
            tenantId, reportNo, cmd.ReportType, cmd.FundCluster, cmd.Station, cmd.AsAt, officer);

        db.UnserviceablePropertyReports.Add(report);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class AddUnserviceableReportItemCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<AddUnserviceableReportItemCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        AddUnserviceableReportItemCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");
        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == cmd.AssetRegistryId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{cmd.AssetRegistryId}' not found.");

        report.AddItem(asset, cmd.Remarks);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class SubmitUnserviceableReportCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<SubmitUnserviceableReportCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        SubmitUnserviceableReportCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");
        var approvedBy = EmployeeRef.Create(cmd.ApprovedBy.EmployeeId, cmd.ApprovedBy.PrintedName, cmd.ApprovedBy.Designation);

        report.Submit(approvedBy);

        // Mirror lifecycle: each asset → Unserviceable.
        var assetIds = report.Items.Select(i => i.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(ct).ConfigureAwait(false);
        foreach (var asset in assets)
            asset.MarkUnserviceable(report.Id);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class RecordUnserviceableInspectionCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RecordUnserviceableInspectionCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        RecordUnserviceableInspectionCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");

        var inspectedBy = EmployeeRef.Create(cmd.InspectedBy.EmployeeId, cmd.InspectedBy.PrintedName, cmd.InspectedBy.Designation);
        EmployeeRef? witnessedBy = cmd.WitnessedBy is null ? null
            : EmployeeRef.Create(cmd.WitnessedBy.EmployeeId, cmd.WitnessedBy.PrintedName, cmd.WitnessedBy.Designation);

        report.RecordInspection(inspectedBy, cmd.InspectedOn, witnessedBy, cmd.WitnessedOn,
            cmd.Decisions.Select(d => (d.ItemId, d.Method, d.OtherSpecify, d.AppraisedValue)));

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class RecordUnserviceableDisposalCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RecordUnserviceableDisposalCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        RecordUnserviceableDisposalCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");

        report.RecordDisposal(cmd.Records.Select(r => (r.ItemId, r.DisposalRecordedOn, r.SaleORNo, r.SaleAmount)));

        // Mirror lifecycle: each disposed item's asset → Disposed.
        foreach (var item in report.Items.Where(i => i.DisposalRecordedOn.HasValue))
        {
            var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == item.AssetRegistryId, ct).ConfigureAwait(false);
            if (asset is not null && asset.LifecycleState != Contracts.v1.LifecycleState.Disposed)
                asset.Dispose(report.Id, item.DisposalMethod ?? Contracts.v1.DisposalMethod.Other);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class CloseUnserviceableReportCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<CloseUnserviceableReportCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        CloseUnserviceableReportCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");
        report.Close();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class GetUnserviceableReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetUnserviceableReportQuery, UnserviceablePropertyReportDto?>
{
    public async ValueTask<UnserviceablePropertyReportDto?> Handle(
        GetUnserviceableReportQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var report = await db.UnserviceablePropertyReports
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == query.Id, ct).ConfigureAwait(false);
        return report is null ? null : UnserviceableMapper.ToDto(report);
    }
}

public sealed class SearchUnserviceableReportsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchUnserviceableReportsQuery, FSH.Framework.Shared.Persistence.PagedResponse<UnserviceablePropertyReportSummaryDto>>
{
    public async ValueTask<FSH.Framework.Shared.Persistence.PagedResponse<UnserviceablePropertyReportSummaryDto>> Handle(
        SearchUnserviceableReportsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.UnserviceablePropertyReports.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(r => r.ReportNo.ToLower().Contains(k));
        }
        if (query.ReportType.HasValue) q = q.Where(r => r.ReportType == query.ReportType.Value);
        if (query.Status.HasValue) q = q.Where(r => r.Status == query.Status.Value);
        if (query.FromDate.HasValue) q = q.Where(r => r.AsAt >= query.FromDate.Value);
        if (query.ToDate.HasValue) q = q.Where(r => r.AsAt <= query.ToDate.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderByDescending(r => r.AsAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(r => new UnserviceablePropertyReportSummaryDto(
                r.Id, r.ReportNo, r.ReportType, r.Status, r.AsAt, r.Items.Count))
            .ToListAsync(ct).ConfigureAwait(false);

        return new FSH.Framework.Shared.Persistence.PagedResponse<UnserviceablePropertyReportSummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}
