using Mediator;

namespace AMIS.Modules.Reporting.Features.v1.PurchaseRequests.PrintPurchaseRequestFast;

public sealed record PrintPurchaseRequestFastQuery(
    Guid Id,
    string PaperSize = "a4",
    int Copies = 2,
    string Orientation = "landscape") : IQuery<byte[]>;
