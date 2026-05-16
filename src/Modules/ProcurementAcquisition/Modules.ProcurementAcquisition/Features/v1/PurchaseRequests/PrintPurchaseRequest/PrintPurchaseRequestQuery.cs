using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.PrintPurchaseRequest;

internal sealed record PrintPurchaseRequestQuery(
    Guid Id,
    string? PageWidth = null,
    string? PageHeight = null) : IQuery<byte[]>;
