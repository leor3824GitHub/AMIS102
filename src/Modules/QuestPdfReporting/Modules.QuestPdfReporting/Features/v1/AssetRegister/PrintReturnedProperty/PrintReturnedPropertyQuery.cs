using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintReturnedProperty;

/// <summary>
/// Renders a Receipt of Returned Property as a PDF (NFA Exhibit 6, SOP GS-PD16).
/// RRP = Receipt of Returned Property (PPE / PAR); RRSP = Receipt of Returned
/// Semi-Expendable Property (SE / ICS). The receipt type on the document decides
/// the title and the accountability-document column label (PAR No. vs ICS No.).
/// </summary>
public sealed record PrintReturnedPropertyQuery(
    Guid ReceiptId,
    string PaperSize = "longbond",
    string Orientation = "portrait",
    double Margin = 14d,
    int MinRows = 12) : IQuery<byte[]>;
