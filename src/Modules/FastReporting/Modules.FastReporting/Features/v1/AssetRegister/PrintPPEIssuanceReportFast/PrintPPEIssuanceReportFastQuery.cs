using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintPPEIssuanceReportFast;

public sealed record PrintPPEIssuanceReportFastQuery(
    Guid Id,
    string PaperSize = "longbond",
    string Orientation = "portrait",
    int MinRows = 12,
    bool DataOnly = false,
    float OffsetXmm = 0f,
    float OffsetYmm = 0f) : IQuery<ReportFileDto>;
