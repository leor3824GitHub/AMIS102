using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.DeleteFundingSourceCode;

public sealed class DeleteFundingSourceCodeCommandHandler : ICommandHandler<DeleteFundingSourceCodeCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteFundingSourceCodeCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteFundingSourceCodeCommand command, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.FundingSourceCodes
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new NotFoundException($"Funding source code with ID {command.Id} not found.");
        }

        entity.SoftDelete(_currentUser.GetUserId().ToString());

        _dbContext.FundingSourceCodes.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
