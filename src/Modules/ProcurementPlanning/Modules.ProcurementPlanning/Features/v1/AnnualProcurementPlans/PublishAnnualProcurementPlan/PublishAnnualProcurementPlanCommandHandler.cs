using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Domain.Ppmps;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PublishAnnualProcurementPlan;

public sealed class PublishAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext,
    FSH.Framework.Core.Context.ICurrentUser currentUser) : ICommandHandler<PublishAnnualProcurementPlanCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        PublishAnnualProcurementPlanCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.LineReferences)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {command.Id} not found.");

        var snapshotLines = await (
                from line in dbContext.AppLineReferences.AsNoTracking()
                join ppmp in dbContext.Ppmps.AsNoTracking() on line.SourcePpmpId equals ppmp.Id
                join ppmpItem in dbContext.PpmpItems.AsNoTracking() on line.SourcePpmpItemId equals ppmpItem.Id
                where line.AppId == app.Id
                orderby line.ItemNo
                select new AppSnapshotLineData(
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
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        app.Publish();

        var existingPublishedSnapshot = await dbContext.AppSnapshots
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.AppId == app.Id &&
                     x.VersionNumber == app.VersionNumber &&
                     x.SnapshotType == AppSnapshotType.Published,
                cancellationToken)
            .ConfigureAwait(false);

        if (existingPublishedSnapshot is not null)
        {
            dbContext.AppSnapshots.Remove(existingPublishedSnapshot);
        }

        var capturedBy = currentUser.GetUserId().ToString();
        var publishedSnapshot = AppSnapshot.Capture(app, AppSnapshotType.Published, capturedBy, snapshotLines);
        dbContext.AppSnapshots.Add(publishedSnapshot);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
    }
}
