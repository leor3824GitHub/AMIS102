using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetPhysicalCountSessionById;

public sealed record GetPhysicalCountSessionByIdQuery(Guid Id) : IQuery<PhysicalCountSessionDetailsDto>;

public sealed record PhysicalCountSessionDetailsDto(
    Guid Id,
    string SessionNo,
    DateOnly CountDate,
    string StationOffice,
    string Scope,
    string Status,
    Guid PreparedByEmployeeId,
    Guid CertifiedByEmployeeId,
    Guid ApprovedByEmployeeId,
    DateTimeOffset? SubmittedOnUtc,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<PhysicalCountEntryDto> Entries);

public sealed record PhysicalCountEntryDto(
    Guid Id,
    Guid? PPEItemId,
    Guid? SemiExpendablePropertyId,
    string PropertyNumber,
    string Description,
    decimal UnitCost,
    string? Result,
    string? Condition,
    int QuantityOnHand,
    string? Remarks,
    bool IsScanned,
    DateTimeOffset? ScannedOnUtc,
    string? PhotoPath);
