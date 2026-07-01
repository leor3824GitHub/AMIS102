using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPtr;

/// <summary>
/// Renders the Property Transfer Report (PTR) PDF for a single PPEIR. PTR has no separate entity —
/// it is the PPEIR re-laid-out as a transfer form, so it reuses the issuance report document data.
/// </summary>
public sealed record PrintPtrQuery(
    Guid ReportId,
    string PaperSize = "a4",
    string Orientation = "portrait",
    double Margin = 12d) : IQuery<byte[]>;
