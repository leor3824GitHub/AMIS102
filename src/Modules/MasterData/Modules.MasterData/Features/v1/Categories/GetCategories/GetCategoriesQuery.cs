using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.Categories.GetCategories;

public sealed record GetCategoriesQuery(
    string? Keyword = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponseOfCategoryDto>;

public sealed record PagedResponseOfCategoryDto(
    ICollection<CategoryDto>? Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record CategoryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    string? OfficeCode = null);

