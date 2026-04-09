using Mediator;

namespace FSH.Modules.MasterData.Features.v1.Suppliers.CreateSupplier;

public sealed record CreateSupplierCommand(
    string Code,
    string Name,
    string? TinNo = null,
    string BusinessTaxType = "NON-VAT",
    string? Description = null,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null) : ICommand<SupplierDto>;

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
