using Mediator;

namespace AMIS.Modules.RdlcReporting.Features.v1.PurchaseRequests.PrintPurchaseRequest;

public sealed record PrintPurchaseRequestQuery(
    Guid Id,
    string? PageWidth = null,
    string? PageHeight = null) : IQuery<byte[]>;
