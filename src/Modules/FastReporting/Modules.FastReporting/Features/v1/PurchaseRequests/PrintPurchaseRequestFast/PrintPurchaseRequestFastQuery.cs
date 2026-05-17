using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.PurchaseRequests.PrintPurchaseRequestFast;

public sealed record PrintPurchaseRequestFastQuery(
    Guid Id,
    string PaperSize = "a4",
    int Copies = 2,
    string Orientation = "landscape",
    int MinRows = 10) : IQuery<ReportFileDto>;
