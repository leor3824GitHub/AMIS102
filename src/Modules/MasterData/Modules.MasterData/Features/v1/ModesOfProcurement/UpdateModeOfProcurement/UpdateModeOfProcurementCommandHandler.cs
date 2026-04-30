using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.UpdateModeOfProcurement;

public sealed class UpdateModeOfProcurementCommandHandler : ICommandHandler<UpdateModeOfProcurementCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateModeOfProcurementCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdateModeOfProcurementCommand command, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ModesOfProcurement
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new KeyNotFoundException($"Mode of procurement with ID {command.Id} not found.");
        }

        var nameInUse = await _dbContext.ModesOfProcurement
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Name == command.Name && x.Id != command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (nameInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Name), "A mode of procurement with this name already exists.")
            ]);
        }

        entity.Update(command.Name, command.Description, command.IsActive);
        entity.LastModifiedBy = _currentUser.GetUserId().ToString();

        _dbContext.ModesOfProcurement.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
