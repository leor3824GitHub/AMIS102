using AMIS.Modules.AssetManagement.Domain;
using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.CreateRRP;

public sealed record CreateRRPCommand(
    string RRPNo,
    DateOnly Date,
    PPEReturnCategory ReturnCategory,
    Guid ReturnedByEmployeeId,
    Guid ApprovedByEmployeeId,
    Guid SignedByEmployeeId,
    bool PropertyInspectorCertified,
    IReadOnlyList<CreateRRPItemRequest> Items) : ICommand<CreateRRPResult>;

public sealed record CreateRRPItemRequest(
    Guid TangibleInventoryItemId,
    string? SourceDocumentRef,
    int Quantity);

public sealed record CreateRRPResult(
    Guid RRPId,
    string RRPNo,
    int ItemCount);

