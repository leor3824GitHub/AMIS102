using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.AssetRegister.Contracts.v1.Catalog;

public sealed record PropertyItemCatalogDto(
    Guid Id,
    string Code,
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears,
    bool IsActive);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record CreatePropertyItemCatalogCommand(
    string Code,
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears) : ICommand<PropertyItemCatalogDto>;

public sealed record UpdatePropertyItemCatalogCommand(
    Guid Id,
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears) : ICommand<PropertyItemCatalogDto>;

public sealed record DeletePropertyItemCatalogCommand(Guid Id) : ICommand;

public sealed record SetPropertyItemCatalogActivationCommand(Guid Id, bool IsActive) : ICommand<PropertyItemCatalogDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetPropertyItemCatalogQuery(Guid Id) : IQuery<PropertyItemCatalogDto?>;

public sealed record SearchPropertyItemCatalogsQuery(
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<PropertyItemCatalogDto>>;
