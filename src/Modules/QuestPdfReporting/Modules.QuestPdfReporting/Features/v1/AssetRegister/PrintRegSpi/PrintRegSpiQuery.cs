using AMIS.Modules.AssetRegister.Contracts.v1;
using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRegSpi;

/// <summary>Renders the Registry of Semi-Expendable Property Issued (RegSPI) PDF.</summary>
public sealed record PrintRegSpiQuery(
    DateOnly? AsOfDate,
    AssetType? AssetType,
    Guid? CustodianId,
    string? FundCluster = null,
    string? PropertyClass = null,
    string PaperSize = "legal",
    string Orientation = "landscape",
    double Margin = 12d) : IQuery<byte[]>;
