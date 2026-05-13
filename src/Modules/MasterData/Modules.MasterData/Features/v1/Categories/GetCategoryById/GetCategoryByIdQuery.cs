using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.Categories.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid Id) : IQuery<CategoryDetailsDto>;

public sealed record CategoryDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    string? OfficeCode,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);

