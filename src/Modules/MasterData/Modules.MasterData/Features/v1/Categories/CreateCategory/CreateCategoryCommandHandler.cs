using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Data;
using FSH.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.Categories.CreateCategory;

public sealed class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateCategoryCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CategoryDto> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await _dbContext.Categories
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A category with this code already exists.")
            ]);
        }

        var category = Category.Create(command.Code, command.Name, command.Description, command.OfficeCode);
        category.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CategoryDto(
            category.Id,
            category.Code,
            category.Name,
            category.Description,
            category.IsActive,
            category.OfficeCode);
    }
}
