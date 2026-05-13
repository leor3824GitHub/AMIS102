using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.GetSupplierById;

public sealed record GetSupplierByIdQuery(Guid Id) : IQuery<SupplierDetailsDto>;

public sealed record SupplierDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? TinNo,
    string BusinessTaxType,
    string? Description,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive,
    string? OfficeCode,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);

