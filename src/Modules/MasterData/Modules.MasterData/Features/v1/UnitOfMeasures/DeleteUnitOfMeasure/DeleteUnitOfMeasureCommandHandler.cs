using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.UnitOfMeasures.DeleteUnitOfMeasure;

public sealed class DeleteUnitOfMeasureCommandHandler : ICommandHandler<DeleteUnitOfMeasureCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteUnitOfMeasureCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteUnitOfMeasureCommand command, CancellationToken cancellationToken)
    {
        var unitOfMeasure = await _dbContext.UnitOfMeasures
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"UnitOfMeasure {command.Id} not found.");

        unitOfMeasure.IsDeleted = true;
        unitOfMeasure.DeletedOnUtc = DateTimeOffset.UtcNow;
        unitOfMeasure.DeletedBy = _currentUser.GetUserId().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
