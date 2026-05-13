using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Categories.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler : IQueryHandler<GetCategoryByIdQuery, CategoryDetailsDto>
{
    private readonly MasterDataDbContext _dbContext;

    public GetCategoryByIdQueryHandler(MasterDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<CategoryDetailsDto> Handle(GetCategoryByIdQuery query, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
        {
            throw new KeyNotFoundException($"Category with ID {query.Id} not found.");
        }

        return new CategoryDetailsDto(
            category.Id,
            category.Code,
            category.Name,
            category.Description,
            category.IsActive,
            category.OfficeCode,
            category.CreatedOnUtc,
            category.CreatedBy,
            category.LastModifiedOnUtc,
            category.LastModifiedBy);
    }
}

