using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.CreateSMIR;

public sealed record CreateSMIRCommand(
    string SMIRNo,
    DateOnly Date,
    string? FundCluster,
    SMIRIssuanceType IssuanceType,
    string? TransferredToTenantId,
    string? TransferredToOfficerName,
    Guid? IssuedByEmployeeId,
    string? Remarks,
    IReadOnlyList<CreateSMIRItemRequest> Items) : ICommand<CreateSMIRResult>;

public sealed record CreateSMIRItemRequest(
    Guid TangibleInventoryItemId,
    string? Description);

public sealed record CreateSMIRResult(
    Guid SMIRId,
    string SMIRNo,
    int ItemCount);
