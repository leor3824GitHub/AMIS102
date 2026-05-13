using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Positions.UpdatePosition;

public sealed class UpdatePositionCommandHandler : ICommandHandler<UpdatePositionCommand, PositionReferenceDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdatePositionCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<PositionReferenceDto> Handle(UpdatePositionCommand command, CancellationToken cancellationToken)
    {
        var position = await _dbContext.Positions
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Position {command.Id} not found.");

        var codeInUse = await _dbContext.Positions
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != command.Id && x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A position with this code already exists.")
            ]);
        }

        position.Update(command.Code, command.Name, command.Description, command.IsActive);
        position.LastModifiedBy = _currentUser.GetUserId().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PositionReferenceDto(position.Id, position.Code, position.Name, position.Description, position.IsActive, position.OfficeCode);
    }
}
