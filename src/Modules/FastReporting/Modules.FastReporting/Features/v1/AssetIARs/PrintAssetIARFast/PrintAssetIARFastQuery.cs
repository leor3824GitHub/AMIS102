using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.AssetIARs.PrintAssetIARFast;

public sealed record PrintAssetIARFastQuery(
    Guid Id,
    string PaperSize = "a4",
    string Orientation = "portrait",
    int MinRows = 12) : IQuery<ReportFileDto>;
