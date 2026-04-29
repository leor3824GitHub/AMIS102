using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans;

internal static class AppMapper
{
    internal static AnnualProcurementPlanDto ToDto(AnnualProcurementPlan app) =>
        new(app.Id,
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
            app.Items?.Sum(i => i.EstimatedBudget) ?? 0m,
            (app.Items ?? []).Select(i => new AppItemDto(
                i.Id, i.SourcePpmpId, i.SourcePpmpItemId,
                i.OfficeCode, i.EndUserUnit, i.ItemNo,
                i.GeneralDescription, i.ProjectType,
                i.Quantity, i.Unit, i.ModeOfProcurement,
                i.PreProcurementConference,
                i.ProcurementStart, i.ProcurementEnd, i.ExpectedDelivery,
                i.SourceOfFunds, i.EstimatedBudget, i.Remarks))
            .ToList(),
            app.CreatedOnUtc,
            app.CreatedBy,
            app.LastModifiedOnUtc);
}
