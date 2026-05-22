using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintPPEReceivingReportFast;

public sealed record PrintPPEReceivingReportFastQuery(
    Guid Id,
    string PaperSize = "longbond",
    string Orientation = "portrait",
    int MinRows = 12) : IQuery<ReportFileDto>;
