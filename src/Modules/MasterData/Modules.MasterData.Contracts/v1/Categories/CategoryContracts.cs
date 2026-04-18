namespace FSH.Modules.MasterData.Contracts.v1.Categories;

public record CategoryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    string? OfficeCode = null);

public record CreateCategoryCommand(
    string Code,
    string Name,
    string? Description = null,
    string? OfficeCode = null);

public record UpdateCategoryCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description = null,
    bool IsActive = true);

public record GetCategoryQuery(Guid Id);

public record DeleteCategoryCommand(Guid Id);
