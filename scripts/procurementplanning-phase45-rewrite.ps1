$files = @{}
$files['e:\AMIS101\src\Modules\ProcurementPlanning\Modules.ProcurementPlanning\Features\v1\AnnualProcurementPlans\ConsolidatePpmps\ConsolidatePpmpsCommandHandler.cs'] = @'
using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ConsolidatePpmps;

public sealed class ConsolidatePpmpsCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<ConsolidatePpmpsCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        ConsolidatePpmpsCommand command, CancellationToken cancellationToken)
    {
        if (command.PpmpIds is null || command.PpmpIds.Count == 0)
            throw new CustomException("Select at least one PPMP to consolidate.", Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

        var selectedIds = command.PpmpIds.Distinct().ToList();
        var userId = currentUser.GetUserId();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var app = await dbContext.AnnualProcurementPlans
                .Include(x => x.SourcePpmps)
                .Include(x => x.LineItems)
                .FirstOrDefaultAsync(x => x.Id == command.AppId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new CustomException($"APP {command.AppId} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

            var existingSourcePpmpIds = app.SourcePpmps.Select(x => x.PpmpId).ToList();

            var ppmps = await dbContext.Ppmps
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => selectedIds.Contains(x.Id) &&
                            (x.Status == PpmpStatus.Approved ||
                             (x.Status == PpmpStatus.Consolidated && existingSourcePpmpIds.Contains(x.Id))))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var foundIds = ppmps.Select(x => x.Id).ToHashSet();
            var missingIds = selectedIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Count > 0)
                throw new CustomException(
                    "Some selected PPMPs are no longer eligible. Please refresh and try again.",
                    missingIds.Select(x => x.ToString()),
                    HttpStatusCode.Conflict);

            if (ppmps.Count == 0)
                throw new CustomException("No eligible PPMPs found for consolidation.", Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

            try
            {
                app.ConsolidatePpmps(ppmps, userId);
            }
            catch (InvalidOperationException ex)
            {
                throw new CustomException(ex.Message, Enumerable.Empty<string>(), HttpStatusCode.Conflict);
            }

            var approvedIds = ppmps.Where(x => x.Status == PpmpStatus.Approved).Select(x => x.Id).ToList();
            if (approvedIds.Count > 0)
            {
                var ppmpNow = DateTimeOffset.UtcNow;
                var updatedPpmps = await dbContext.Ppmps
                    .Where(x => approvedIds.Contains(x.Id) && x.Status == PpmpStatus.Approved)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, PpmpStatus.Consolidated)
                        .SetProperty(x => x.LastModifiedOnUtc, ppmpNow),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (updatedPpmps != approvedIds.Count)
                    throw new CustomException(
                        "Some selected PPMPs changed state while consolidating. Please refresh and try again.",
                        Enumerable.Empty<string>(),
                        HttpStatusCode.Conflict);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return await AppReadProjection.BuildDtoAsync(dbContext, command.AppId, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
'@
$files['e:\AMIS101\src\Modules\ProcurementPlanning\Modules.ProcurementPlanning\Features\v1\AnnualProcurementPlans\DeleteAnnualProcurementPlan\DeleteAnnualProcurementPlanCommandHandler.cs'] = @'
using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.DeleteAnnualProcurementPlan;

public sealed class DeleteAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<DeleteAnnualProcurementPlanCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteAnnualProcurementPlanCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.SourcePpmps)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"APP {command.Id} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        if (app.Status != AppStatus.Draft)
            throw new CustomException(
                "Only Draft APPs can be deleted. Published or Approved APPs must be recalled or amended.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        var consolidatedPpmpIds = app.SourcePpmps.Select(i => i.PpmpId).Distinct().ToList();
        if (consolidatedPpmpIds.Count > 0)
        {
            var ppmps = await dbContext.Ppmps
                .Where(x => consolidatedPpmpIds.Contains(x.Id) && x.Status == PpmpStatus.Consolidated)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var ppmp in ppmps)
                ppmp.UnmarkConsolidated();
        }

        app.IsDeleted = true;
        app.DeletedOnUtc = DateTimeOffset.UtcNow;
        app.DeletedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
'@
$files['e:\AMIS101\src\Modules\ProcurementPlanning\Modules.ProcurementPlanning\Features\v1\AnnualProcurementPlans\AppReadProjection.cs'] = @'
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans;

internal static class AppReadProjection
{
    internal static async Task<AnnualProcurementPlanDto> BuildDtoAsync(
        ProcurementPlanningDbContext dbContext,
        Guid appId,
        CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == appId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {appId} not found.");

        var items = await dbContext.AppLineItems
            .AsNoTracking()
            .Where(x => x.AppId == app.Id)
            .OrderBy(x => x.ItemNo)
            .Select(x => new AppItemDto(
                x.Id,
                x.SourcePpmpId,
                x.SourcePpmpItemId,
                x.OfficeCode,
                x.EndUserUnit,
                x.ItemNo,
                x.GeneralDescription,
                x.ProjectType,
                x.Quantity,
                x.Unit,
                x.ModeOfProcurement,
                x.PreProcurementConference,
                x.ProcurementStart,
                x.ProcurementEnd,
                x.ExpectedDelivery,
                x.SourceOfFunds,
                x.EstimatedBudget,
                x.Remarks))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AnnualProcurementPlanDto(
            app.Id,
            app.AppNumber,
            app.FiscalYear,
            app.Phase,
            app.Status,
            app.VersionNumber,
            app.IsCurrentVersion,
            app.VersionChainId,
            app.PreviousVersionId,
            app.AmendmentReason,
            app.AmendedAt,
            app.AmendedById,
            app.ConsolidatedById,
            app.ConsolidatedOn,
            app.ApprovedById,
            app.ApprovedOn,
            app.ReturnReason,
            app.ReturnedAt,
            app.ReturnedById,
            items.Sum(x => x.EstimatedBudget),
            items,
            app.CreatedOnUtc,
            app.CreatedBy,
            app.LastModifiedOnUtc);
    }
}
'@
$files['e:\AMIS101\src\Modules\ProcurementPlanning\Modules.ProcurementPlanning\Features\v1\AnnualProcurementPlans\SearchAnnualProcurementPlans\SearchAnnualProcurementPlansQueryHandler.cs'] = @'
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.SearchAnnualProcurementPlans;

public sealed class SearchAnnualProcurementPlansQueryHandler(
    ProcurementPlanningDbContext dbContext) : IQueryHandler<SearchAnnualProcurementPlansQuery, PagedResponse<AnnualProcurementPlanSummaryDto>>
{
    public async ValueTask<PagedResponse<AnnualProcurementPlanSummaryDto>> Handle(
        SearchAnnualProcurementPlansQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.AnnualProcurementPlans
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (query.CurrentVersionOnly)
            q = q.Where(x => x.IsCurrentVersion);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.AppNumber.Contains(query.Keyword));

        if (query.FiscalYear.HasValue)
            q = q.Where(x => x.FiscalYear == query.FiscalYear.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.Phase.HasValue)
            q = q.Where(x => x.Phase == query.Phase.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageApps = await q
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new
            {
                x.Id,
                x.AppNumber,
                x.FiscalYear,
                x.Phase,
                x.Status,
                x.VersionNumber,
                x.IsCurrentVersion,
                x.VersionChainId,
                x.CreatedOnUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var appIds = pageApps.Select(x => x.Id).ToList();

        var aggregates = await (
                from line in dbContext.AppLineItems.AsNoTracking()
                where appIds.Contains(line.AppId)
                group line by line.AppId
                into g
                select new
                {
                    AppId = g.Key,
                    ItemCount = g.Count(),
                    TotalEstimatedBudget = g.Sum(x => x.EstimatedBudget)
                })
            .ToDictionaryAsync(x => x.AppId, cancellationToken)
            .ConfigureAwait(false);

        var items = pageApps
            .Select(x =>
            {
                var hasAgg = aggregates.TryGetValue(x.Id, out var agg);
                return new AnnualProcurementPlanSummaryDto(
                    x.Id,
                    x.AppNumber,
                    x.FiscalYear,
                    x.Phase,
                    x.Status,
                    x.VersionNumber,
                    x.IsCurrentVersion,
                    x.VersionChainId,
                    hasAgg ? agg!.ItemCount : 0,
                    hasAgg ? agg!.TotalEstimatedBudget : 0,
                    x.CreatedOnUtc);
            })
            .ToList();

        return new PagedResponse<AnnualProcurementPlanSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}
'@
$files['e:\AMIS101\src\Modules\ProcurementPlanning\Modules.ProcurementPlanning\Features\v1\AnnualProcurementPlans\GetAppVersions\GetAppVersionsQueryHandler.cs'] = @'
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAppVersions;

public sealed class GetAppVersionsQueryHandler(
    ProcurementPlanningDbContext dbContext) : IQueryHandler<GetAppVersionsQuery, IReadOnlyList<AnnualProcurementPlanSummaryDto>>
{
    public async ValueTask<IReadOnlyList<AnnualProcurementPlanSummaryDto>> Handle(
        GetAppVersionsQuery query, CancellationToken cancellationToken)
    {
        var versions = await dbContext.AnnualProcurementPlans
            .AsNoTracking()
            .Where(x => x.VersionChainId == query.VersionChainId)
            .OrderBy(x => x.VersionNumber)
            .Select(x => new
            {
                x.Id,
                x.AppNumber,
                x.FiscalYear,
                x.Phase,
                x.Status,
                x.VersionNumber,
                x.IsCurrentVersion,
                x.VersionChainId,
                x.CreatedOnUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var appIds = versions.Select(x => x.Id).ToList();

        var aggregates = await (
                from line in dbContext.AppLineItems.AsNoTracking()
                where appIds.Contains(line.AppId)
                group line by line.AppId
                into g
                select new
                {
                    AppId = g.Key,
                    ItemCount = g.Count(),
                    TotalEstimatedBudget = g.Sum(x => x.EstimatedBudget)
                })
            .ToDictionaryAsync(x => x.AppId, cancellationToken)
            .ConfigureAwait(false);

        return versions
            .Select(x =>
            {
                var hasAgg = aggregates.TryGetValue(x.Id, out var agg);
                return new AnnualProcurementPlanSummaryDto(
                    x.Id,
                    x.AppNumber,
                    x.FiscalYear,
                    x.Phase,
                    x.Status,
                    x.VersionNumber,
                    x.IsCurrentVersion,
                    x.VersionChainId,
                    hasAgg ? agg!.ItemCount : 0,
                    hasAgg ? agg!.TotalEstimatedBudget : 0,
                    x.CreatedOnUtc);
            })
            .ToList();
    }
}
'@
$files['e:\AMIS101\src\Modules\ProcurementPlanning\Modules.ProcurementPlanning\Features\v1\AnnualProcurementPlans\PublishAnnualProcurementPlan\PublishAnnualProcurementPlanCommandHandler.cs'] = @'
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PublishAnnualProcurementPlan;

public sealed class PublishAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext,
    AMIS.Framework.Core.Context.ICurrentUser currentUser) : ICommandHandler<PublishAnnualProcurementPlanCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        PublishAnnualProcurementPlanCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {command.Id} not found.");

        app.Publish();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
    }
}
'@
$files['e:\AMIS101\src\Modules\ProcurementPlanning\Modules.ProcurementPlanning\Features\v1\AnnualProcurementPlans\ApproveAnnualProcurementPlan\ApproveAnnualProcurementPlanCommandHandler.cs'] = @'
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;

public sealed class ApproveAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<ApproveAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(ApproveAppCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {command.Id} not found.");

        app.Approve(command.ApprovedById);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
    }
}
'@
foreach ($entry in $files.GetEnumerator()) {
    [System.IO.File]::WriteAllText($entry.Key, $entry.Value, [System.Text.Encoding]::UTF8)
}
Write-Output ("Updated {0} files." -f $files.Count)

