using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.BudgetDisbursement.PrintBudgetUtilizationRequest;

/// <summary>
/// Renders a Budget Utilization Request and Status (BURS) as a PDF, following the NFA form layout
/// (Appendix 14): title/serial band, Payee/Office/Address band, Particulars &amp; Amount table, the
/// Certified (A) / Certified (B) signatory boxes, and the Status of Utilization grid.
/// </summary>
public sealed record PrintBudgetUtilizationRequestQuery(
    Guid BurId,
    string PaperSize = "a4",
    string Orientation = "portrait",
    double Margin = 14d) : IQuery<byte[]>;
