using AMIS.Modules.AssetManagement.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetManagement.PrintRegSPI;

public sealed record PrintRegSPIQuery(
    Guid       EmployeeId,
    AssetType? AssetType,
    ICSStatus? Status,
    int        PageNumber  = 1,
    int        PageSize    = 10000,
    string     PaperSize   = "a4",
    string     Orientation = "landscape") : IQuery<byte[]>;
