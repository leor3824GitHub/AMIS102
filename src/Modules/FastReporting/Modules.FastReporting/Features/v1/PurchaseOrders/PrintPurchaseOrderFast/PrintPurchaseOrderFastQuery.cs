using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.PurchaseOrders.PrintPurchaseOrderFast;

public sealed record PrintPurchaseOrderFastQuery(
    Guid Id,
    string PaperSize = "a4",
    string Orientation = "portrait",
    int MinRows = 8) : IQuery<ReportFileDto>;
