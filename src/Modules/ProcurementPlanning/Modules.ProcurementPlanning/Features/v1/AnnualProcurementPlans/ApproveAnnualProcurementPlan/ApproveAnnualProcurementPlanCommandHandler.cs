using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;

public sealed class ApproveAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<ApproveAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(ApproveAppCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.LineReferences)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {command.Id} not found.");

<<<<<<< HEAD
        app.Approve(command.ApprovedById);
=======
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

        app.Approve(command.ApprovedById.ToString());

        var existingApprovedSnapshot = await dbContext.AppSnapshots
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.AppId == app.Id &&
                     x.VersionNumber == app.VersionNumber &&
                     x.SnapshotType == AppSnapshotType.Approved,
                cancellationToken)
            .ConfigureAwait(false);

        if (existingApprovedSnapshot is not null)
        {
            dbContext.AppSnapshots.Remove(existingApprovedSnapshot);
        }

        var approvedSnapshot = AppSnapshot.Capture(app, AppSnapshotType.Approved, command.ApprovedById.ToString(), snapshotLines);
        dbContext.AppSnapshots.Add(approvedSnapshot);

>>>>>>> d63aec54a5aea0527fd07e545543a98aceae4138
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
    }
}
