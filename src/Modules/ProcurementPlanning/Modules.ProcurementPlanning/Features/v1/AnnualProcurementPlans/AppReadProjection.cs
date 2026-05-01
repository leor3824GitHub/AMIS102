using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
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