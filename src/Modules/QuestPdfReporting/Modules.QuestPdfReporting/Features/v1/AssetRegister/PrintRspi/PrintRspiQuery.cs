using AMIS.Modules.AssetRegister.Contracts.v1;
using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRspi;

/// <summary>Renders the Report of Semi-Expendable Property Issued (RSPI) PDF — SE property issued via ICS.</summary>
public sealed record PrintRspiQuery(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    AssetType? AssetType,
    bool ActiveOnly = true,
    string PaperSize = "a4",
    string Orientation = "landscape",
    double Margin = 12d) : IQuery<byte[]>;
