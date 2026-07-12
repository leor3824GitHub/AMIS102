using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Catalog;

/// <summary>
/// Lifecycle status for a <c>PropertyItemCatalog</c> row.
/// Items created inline from a PR start as <see cref="Draft"/> (no UACS yet);
/// an Accountant's "Funds Available" certification back-fills UACS and promotes the row to <see cref="Ready"/>.
/// </summary>
public enum CatalogItemStatus
{
    Draft = 0,
    Ready = 1
}

public sealed record PropertyItemCatalogDto(
    Guid Id,
    string Code,
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears,
    bool IsActive,
    CatalogItemStatus Status = CatalogItemStatus.Ready,
    decimal ResidualValuePercent = 5m,
    DepreciationMethod DepreciationMethod = DepreciationMethod.StraightLine);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record CreatePropertyItemCatalogCommand(
    string Code,
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears,
    decimal ResidualValuePercent = 5m,
    DepreciationMethod DepreciationMethod = DepreciationMethod.StraightLine) : ICommand<PropertyItemCatalogDto>;

public sealed record UpdatePropertyItemCatalogCommand(
    Guid Id,
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears,
    decimal ResidualValuePercent = 5m,
    DepreciationMethod DepreciationMethod = DepreciationMethod.StraightLine) : ICommand<PropertyItemCatalogDto>;

public sealed record DeletePropertyItemCatalogCommand(Guid Id) : ICommand;

public sealed record SetPropertyItemCatalogActivationCommand(Guid Id, bool IsActive) : ICommand<PropertyItemCatalogDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetPropertyItemCatalogQuery(Guid Id) : IQuery<PropertyItemCatalogDto?>;

public sealed record SearchPropertyItemCatalogsQuery(
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<PropertyItemCatalogDto>>;

/// <summary>
/// Resolves many catalog entries in one round-trip. Backs the "by-ids" batch endpoint that replaces
/// per-line-item catalog fetches when a consumer (e.g. the PR edit form) needs to hydrate several rows.
/// Returns only the ids that exist; missing/deleted ids are simply absent from the result.
/// </summary>
public sealed record GetPropertyItemCatalogsByIdsQuery(IReadOnlyCollection<Guid> Ids)
    : IQuery<IReadOnlyList<PropertyItemCatalogDto>>;

