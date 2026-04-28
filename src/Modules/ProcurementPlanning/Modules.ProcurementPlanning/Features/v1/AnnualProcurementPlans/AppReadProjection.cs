using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans;

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

        var snapshotType = app.Status switch
        {
            AppStatus.Approved => AppSnapshotType.Approved,
            AppStatus.Published => AppSnapshotType.Published,
            _ => (AppSnapshotType?)null
        };

        List<AppItemDto> items;

        if (snapshotType.HasValue)
        {
            var snapshot = await dbContext.AppSnapshots
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(
                    x => x.AppId == app.Id &&
                         x.VersionNumber == app.VersionNumber &&
                         x.SnapshotType == snapshotType.Value,
                    cancellationToken)
                .ConfigureAwait(false);

            if (snapshot is not null)
            {
                items = snapshot.Items
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
                    .ToList();
            }
            else
            {
                items = await BuildLiveItemsAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            items = await BuildLiveItemsAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
        }

        return new AnnualProcurementPlanDto(
            app.Id,
            app.AppNumber,
            app.FiscalYear,
            app.RevisionType,
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

    private static Task<List<AppItemDto>> BuildLiveItemsAsync(
        ProcurementPlanningDbContext dbContext,
        Guid appId,
        CancellationToken cancellationToken) =>
        (from line in dbContext.AppLineReferences.AsNoTracking()
         join ppmp in dbContext.Ppmps.AsNoTracking() on line.SourcePpmpId equals ppmp.Id
         join ppmpItem in dbContext.PpmpItems.AsNoTracking() on line.SourcePpmpItemId equals ppmpItem.Id
         where line.AppId == appId
         orderby line.ItemNo
         select new AppItemDto(
             line.Id,
             line.SourcePpmpId,
             line.SourcePpmpItemId,
             ppmp.OfficeCode,
             ppmp.EndUserUnit,
             line.ItemNo,
             ppmpItem.GeneralDescription,
             ppmpItem.ProjectType,
             ppmpItem.Quantity,
             ppmpItem.Unit,
             ppmpItem.ModeOfProcurement,
             ppmpItem.PreProcurementConference,
             ppmpItem.ProcurementStart,
             ppmpItem.ProcurementEnd,
             ppmpItem.ExpectedDelivery,
             ppmpItem.SourceOfFunds,
             ppmpItem.EstimatedBudget,
             ppmpItem.Remarks))
        .ToListAsync(cancellationToken);
}
