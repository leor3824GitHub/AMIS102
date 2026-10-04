using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintApp;

/// <summary>
/// Renders an Annual Procurement Plan (APP) as a PDF in the GPPB APP form layout (RA 12009):
/// sectioned project rows, EPA / CSE / grand totals, and the BAC Secretariat / BAC Chairperson /
/// HoPE signatures from the "AnnualProcurementPlan" report signatories.
/// </summary>
public sealed record PrintAppQuery(
    Guid AppId,
    string PaperSize = "longbond", // 8.5 × 13 in — the agency's APP paper
    string Orientation = "landscape",
    double Margin = 8d) : IQuery<byte[]>;
