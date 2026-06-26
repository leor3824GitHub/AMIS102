using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Procurement.PrintJobOrder;

/// <summary>
/// Renders a Job Order (JO) as a PDF, following the NFA Job Order form layout (supplier band,
/// Particulars/Amount, penalty &amp; performance-bond terms, signatures, Funds Available BUR, and the
/// Inspection / Acceptance boxes).
/// </summary>
public sealed record PrintJobOrderQuery(
    Guid JobOrderId,
    string PaperSize = "a4",
    string Orientation = "portrait",
    double Margin = 10d) : IQuery<byte[]>;
