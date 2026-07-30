using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Contracts.v1.Unserviceable;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Services;
using AMIS.Modules.AssetRegister.Domain.Unserviceable;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Unserviceable;

public sealed class CreateUnserviceableReportDraftCommandHandler(
    AssetRegisterDbContext db, IUnserviceableReportNumberGenerator numbers)
    : ICommandHandler<CreateUnserviceableReportDraftCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        CreateUnserviceableReportDraftCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var reportNo = await numbers.NextAsync(cmd.ReportType, cmd.AsAt, cancellationToken).ConfigureAwait(false);
        var officer = EmployeeRef.Create(
            cmd.AccountableOfficer.EmployeeId, cmd.AccountableOfficer.PrintedName, cmd.AccountableOfficer.Designation);

        var report = UnserviceablePropertyReport.CreateDraft(
            tenantId, reportNo, cmd.ReportType, cmd.Station, cmd.AsAt, officer);

        db.UnserviceablePropertyReports.Add(report);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class AddUnserviceableReportItemCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<AddUnserviceableReportItemCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        AddUnserviceableReportItemCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");
        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == cmd.AssetRegistryId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{cmd.AssetRegistryId}' not found.");

        report.AddItem(asset, cmd.Remarks);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class UpdateUnserviceableReportHeaderCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<UpdateUnserviceableReportHeaderCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        UpdateUnserviceableReportHeaderCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");

        var officer = EmployeeRef.Create(
            cmd.AccountableOfficer.EmployeeId, cmd.AccountableOfficer.PrintedName, cmd.AccountableOfficer.Designation);
        report.UpdateHeader(cmd.Station, cmd.AsAt, officer);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class SubmitUnserviceableReportCommandHandler(AssetRegisterDbContext db, ICountFreezeGuard freezeGuard)
    : ICommandHandler<SubmitUnserviceableReportCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        SubmitUnserviceableReportCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");
        var approvedBy = EmployeeRef.Create(cmd.ApprovedBy.EmployeeId, cmd.ApprovedBy.PrintedName, cmd.ApprovedBy.Designation);

        report.Submit(approvedBy);

        // Mirror lifecycle: each asset → Unserviceable.
        var assetIds = report.Items.Select(i => i.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        await freezeGuard.EnsureMovementAllowedAsync(assets, cancellationToken).ConfigureAwait(false);
        foreach (var asset in assets)
            asset.MarkUnserviceable(report.Id);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class RecordUnserviceableInspectionCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RecordUnserviceableInspectionCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        RecordUnserviceableInspectionCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");

        var inspectedBy = EmployeeRef.Create(cmd.InspectedBy.EmployeeId, cmd.InspectedBy.PrintedName, cmd.InspectedBy.Designation);
        EmployeeRef? witnessedBy = cmd.WitnessedBy is null ? null
            : EmployeeRef.Create(cmd.WitnessedBy.EmployeeId, cmd.WitnessedBy.PrintedName, cmd.WitnessedBy.Designation);

        report.RecordInspection(inspectedBy, cmd.InspectedOn, witnessedBy, cmd.WitnessedOn,
            cmd.Decisions.Select(d => (d.ItemId, d.Method, d.OtherSpecify, d.AppraisedValue)));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class RecordUnserviceableDisposalCommandHandler(AssetRegisterDbContext db, ICountFreezeGuard freezeGuard)
    : ICommandHandler<RecordUnserviceableDisposalCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        RecordUnserviceableDisposalCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");

        report.RecordDisposal(cmd.Records.Select(r => (r.ItemId, r.DisposalRecordedOn, r.SaleORNo, r.SaleAmount)));

        // Mirror lifecycle: each disposed item's asset → Disposed.
        var disposedItems = report.Items.Where(i => i.DisposalRecordedOn.HasValue).ToList();
        var disposedAssetIds = disposedItems.Select(i => i.AssetRegistryId).ToList();
        var disposedAssets = await db.AssetRegistries
            .Where(a => disposedAssetIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken).ConfigureAwait(false);
        await freezeGuard.EnsureMovementAllowedAsync(disposedAssets.Values.ToList(), cancellationToken).ConfigureAwait(false);
        foreach (var item in disposedItems)
        {
            if (disposedAssets.TryGetValue(item.AssetRegistryId, out var asset)
                && asset.LifecycleState != Contracts.v1.LifecycleState.Disposed)
                asset.Dispose(report.Id, item.DisposalMethod ?? Contracts.v1.DisposalMethod.Other);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class CloseUnserviceableReportCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<CloseUnserviceableReportCommand, UnserviceablePropertyReportDto>
{
    public async ValueTask<UnserviceablePropertyReportDto> Handle(
        CloseUnserviceableReportCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.UnserviceablePropertyReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{cmd.ReportId}' not found.");
        report.Close();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return UnserviceableMapper.ToDto(report);
    }
}

public sealed class GetUnserviceableReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetUnserviceableReportQuery, UnserviceablePropertyReportDto?>
{
    public async ValueTask<UnserviceablePropertyReportDto?> Handle(
        GetUnserviceableReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var report = await db.UnserviceablePropertyReports
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken).ConfigureAwait(false);
        return report is null ? null : UnserviceableMapper.ToDto(report);
    }
}

public sealed class SearchUnserviceableReportsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchUnserviceableReportsQuery, AMIS.Framework.Shared.Persistence.PagedResponse<UnserviceablePropertyReportSummaryDto>>
{
    public async ValueTask<AMIS.Framework.Shared.Persistence.PagedResponse<UnserviceablePropertyReportSummaryDto>> Handle(
        SearchUnserviceableReportsQuery query, CancellationToken cancellationToken)
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

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q.OrderByDescending(r => r.AsAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(r => new UnserviceablePropertyReportSummaryDto(
                r.Id, r.ReportNo, r.ReportType, r.Status, r.AsAt, r.Items.Count,
                r.SignedCopy != null))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new AMIS.Framework.Shared.Persistence.PagedResponse<UnserviceablePropertyReportSummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}

