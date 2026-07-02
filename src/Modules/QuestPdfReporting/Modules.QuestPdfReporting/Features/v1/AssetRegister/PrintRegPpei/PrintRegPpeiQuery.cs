using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRegPpei;

/// <summary>Renders the Registry of Property, Plant and Equipment Issued (RegPPEI) PDF.</summary>
public sealed record PrintRegPpeiQuery(
    DateOnly? AsOfDate,
    Guid? CustodianId,
    string? FundCluster = null,
    string? PropertyClass = null,
    string PaperSize = "legal",
    string Orientation = "landscape",
    double Margin = 12d) : IQuery<byte[]>;
