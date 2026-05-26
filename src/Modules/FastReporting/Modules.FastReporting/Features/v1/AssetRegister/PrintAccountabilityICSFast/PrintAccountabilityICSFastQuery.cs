using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintAccountabilityICSFast;

public sealed record PrintAccountabilityICSFastQuery(
    Guid Id,
    string PaperSize = "longbond",
    string Orientation = "portrait",
    int MinRows = 15) : IQuery<ReportFileDto>;
