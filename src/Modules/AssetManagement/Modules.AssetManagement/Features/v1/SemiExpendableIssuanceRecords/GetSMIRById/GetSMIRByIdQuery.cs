using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.GetSMIRById;

public sealed record GetSMIRByIdQuery(Guid Id) : IQuery<SMIRDetailsDto>;

public sealed record SMIRDetailsDto(
    Guid Id,
    string SMIRNo,
    DateOnly Date,
    string? FundCluster,
    string IssuanceType,
    string? TransferredToTenantId,
    string? TransferredToOfficerName,
    Guid? IssuedByEmployeeId,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<SMIRItemDetailsDto> Items);

public sealed record SMIRItemDetailsDto(
    Guid Id,
    Guid SemiExpendablePropertyId,
    string PropertyNo,
    int ItemNo,
    string? Description,
    decimal UnitCost,
    string CategoryAtTimeOfIssuance);
