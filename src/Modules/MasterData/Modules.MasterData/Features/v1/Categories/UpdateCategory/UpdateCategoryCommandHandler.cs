using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateCategoryCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
        {
            throw new KeyNotFoundException($"Category with ID {command.Id} not found.");
        }

        var codeInUse = await _dbContext.Categories
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code && x.Id != command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A category with this code already exists.")
            ]);
        }

        category.Update(command.Code, command.Name, command.Description, command.IsActive);
        category.LastModifiedBy = _currentUser.GetUserId().ToString();

        _dbContext.Categories.Update(category);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

