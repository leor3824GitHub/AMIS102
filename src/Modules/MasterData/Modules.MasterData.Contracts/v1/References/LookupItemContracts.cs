using AMIS.Modules.MasterData.Contracts.v1.Categories;
using Mediator;

namespace AMIS.Modules.MasterData.Contracts.v1.References;

// Unpaged "all active" lookups for bounded reference-data dropdowns. Returning the full
// reference DTOs (rather than a slimmed item) keeps existing UI bindings unchanged while
// removing the silent 100-row page-size truncation the paged lookups suffer from.
public sealed record ListAllDepartmentsQuery : IQuery<IReadOnlyList<DepartmentReferenceDto>>;

public sealed record ListAllOfficesQuery : IQuery<IReadOnlyList<OfficeReferenceDto>>;

public sealed record ListAllPositionsQuery : IQuery<IReadOnlyList<PositionReferenceDto>>;

public sealed record ListAllUnitOfMeasuresQuery : IQuery<IReadOnlyList<UnitOfMeasureReferenceDto>>;

public sealed record ListAllCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;
