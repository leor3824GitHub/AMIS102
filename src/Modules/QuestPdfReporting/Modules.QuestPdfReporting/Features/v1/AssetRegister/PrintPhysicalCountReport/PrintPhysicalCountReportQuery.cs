using AMIS.Modules.AssetRegister.Contracts.v1;
using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPhysicalCountReport;

/// <summary>
/// Renders the COA Report on the Physical Count of property for a session, scoped to one asset type:
/// PPE → RPCPPE, Semi-Expendable → RPCSEMEX. Sourced from the AssetRegister physical-count report view.
/// </summary>
public sealed record PrintPhysicalCountReportQuery(
    Guid SessionId,
    AssetType AssetType,
    string PaperSize = "a4",
    string Orientation = "landscape",
    double Margin = 12d) : IQuery<byte[]>;
