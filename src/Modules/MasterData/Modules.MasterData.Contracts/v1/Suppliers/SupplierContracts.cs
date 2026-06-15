using Mediator;

namespace AMIS.Modules.MasterData.Contracts.v1.Suppliers;

/// <summary>Lightweight supplier projection for cross-module reads (e.g. procurement seeding / lookups).</summary>
public sealed record SupplierReferenceDto(Guid Id, string Code, string Name, string? TinNo, string? Address);

/// <summary>Resolves active suppliers for cross-module reference (matched by Code / Name). Returns up to a sensible cap.</summary>
public sealed record ListSupplierReferencesQuery(string? Keyword = null) : IQuery<IReadOnlyList<SupplierReferenceDto>>;

public record SupplierDto(
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
    string? OfficeCode = null);

public record CreateSupplierCommand(
    string Code,
    string Name,
    string? TinNo = null,
    string BusinessTaxType = "NON-VAT",
    string? Description = null,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null,
    bool IsActive = true,
    string? OfficeCode = null);

public record UpdateSupplierCommand(
    Guid Id,
    string Code,
    string Name,
    string? TinNo = null,
    string BusinessTaxType = "NON-VAT",
    string? Description = null,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null,
    bool IsActive = true);

public record GetSupplierQuery(Guid Id);

public record DeleteSupplierCommand(Guid Id);

