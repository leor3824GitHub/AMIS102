using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Domain.Ppmps;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps;

internal static class PpmpMapper
{
    internal static PpmpDto ToDto(Ppmp ppmp) =>
        new(ppmp.Id,
            ppmp.PpmpNumber,
            ppmp.FiscalYear,
            ppmp.PpmpType,
            ppmp.OfficeCode,
            ppmp.EndUserUnit,
            ppmp.Status,
            ppmp.VersionNumber,
            ppmp.IsCurrentVersion,
            ppmp.VersionChainId,
            ppmp.PreviousVersionId,
            ppmp.AmendmentReason,
            ppmp.AmendedAt,
            ppmp.AmendedById,
            ppmp.PreparedById,
            ppmp.SubmittedAt,
            ppmp.ApprovedAt,
            ppmp.ApprovedById,
            ppmp.ReturnReason,
            ppmp.ReturnedAt,
            ppmp.ReturnedById,
            ppmp.AppId,
            ppmp.Items.Sum(i => i.EstimatedBudget),
            ppmp.Items.Select(i => new PpmpItemDto(
                i.Id, i.ItemNo, i.GeneralDescription, i.ProjectType,
                i.Quantity, i.Unit, i.ModeOfProcurement, i.PreProcurementConference,
                i.ProcurementStart, i.ProcurementEnd, i.ExpectedDelivery,
                i.SourceOfFunds, i.EstimatedBudget, i.SupportingDocuments, i.Remarks))
            .ToList(),
            ppmp.CreatedOnUtc,
            ppmp.CreatedBy,
            ppmp.LastModifiedOnUtc);
}
