using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.DeleteModeOfProcurement;

public sealed class DeleteModeOfProcurementCommandHandler : ICommandHandler<DeleteModeOfProcurementCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteModeOfProcurementCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteModeOfProcurementCommand command, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ModesOfProcurement
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new KeyNotFoundException($"Mode of procurement with ID {command.Id} not found.");
        }

        entity.DeletedOnUtc = DateTimeOffset.UtcNow;
        entity.DeletedBy = _currentUser.GetUserId().ToString();
        entity.IsDeleted = true;

        _dbContext.ModesOfProcurement.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
