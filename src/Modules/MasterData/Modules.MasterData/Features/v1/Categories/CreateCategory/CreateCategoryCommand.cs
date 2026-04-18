using Mediator;

namespace FSH.Modules.MasterData.Features.v1.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string Code,
    string Name,
    string? Description = null,
    string? OfficeCode = null) : ICommand<CategoryDto>;

public sealed record CategoryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    string? OfficeCode = null);
