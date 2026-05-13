using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Positions.CreatePosition;

public sealed class CreatePositionCommandHandler : ICommandHandler<CreatePositionCommand, PositionReferenceDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreatePositionCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<PositionReferenceDto> Handle(CreatePositionCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await _dbContext.Positions
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A position with this code already exists.")
            ]);
        }

        var position = Position.Create(command.Code, command.Name, command.Description, command.OfficeCode);
        position.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.Positions.Add(position);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PositionReferenceDto(position.Id, position.Code, position.Name, position.Description, position.IsActive, position.OfficeCode);
    }
}
