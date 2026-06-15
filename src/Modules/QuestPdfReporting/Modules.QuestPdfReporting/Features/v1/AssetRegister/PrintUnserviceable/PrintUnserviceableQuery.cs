using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintUnserviceable;

/// <summary>Renders the Inventory &amp; Inspection Report of Unserviceable Property (IIRUP / IIRUSP).</summary>
public sealed record PrintUnserviceableQuery(
    Guid ReportId,
    string PaperSize = "a4",
    string Orientation = "landscape",
    double Margin = 12d) : IQuery<byte[]>;
