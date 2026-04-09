using Mediator;

namespace FSH.Modules.MasterData.Features.v1.Suppliers.GetSuppliers;

public sealed record GetSuppliersQuery(
    string? Keyword = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponseOfSupplierDto>;

public sealed record PagedResponseOfSupplierDto(
    ICollection<SupplierDto>? Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record SupplierDto(
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
    bool IsActive);
