using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.CreatePPEIR;

public sealed record CreatePPEIRCommand(
    string PPEIRNo,
    DateOnly Date,
    Guid IssuedToEmployeeId,
    string IssuedToOfficeAddress,
    PPEIssuanceType IssuanceType,
    Guid IssuedByEmployeeId,
    Guid ReceivedByEmployeeId,
    Guid ApprovedByEmployeeId,
    DateOnly? DateReceived,
    string? DriverName,
    string? BillOfLadingNo,
    IReadOnlyList<CreatePPEIRItemRequest> Items) : ICommand<CreatePPEIRResult>;

public sealed record CreatePPEIRItemRequest(
    Guid TangibleInventoryItemId);

public sealed record CreatePPEIRResult(
    Guid PPEIRId,
    string PPEIRNo,
    int ItemCount);
