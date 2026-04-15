using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPPEIRById;

public sealed record GetPPEIRByIdQuery(Guid Id) : IQuery<PPEIRDetailsDto>;

public sealed record PPEIRDetailsDto(
    Guid Id,
    string PPEIRNo,
    DateOnly Date,
    Guid IssuedToEmployeeId,
    string IssuedToOfficeAddress,
    string IssuanceType,
    Guid IssuedByEmployeeId,
    Guid ReceivedByEmployeeId,
    DateOnly? DateReceived,
    Guid ApprovedByEmployeeId,
    string? DriverName,
    string? BillOfLadingNo,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<PPEIRItemDto> Items);

public sealed record PPEIRItemDto(
    Guid Id,
    int ItemNo,
    Guid PPEItemId,
    string PropertyCode,
    string? SerialNumber,
    string PPESpecification,
    DateOnly DateAcquired,
    decimal AcquisitionCost,
    decimal? AccumulatedDepreciation,
    decimal? BookValue);
