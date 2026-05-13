using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.ModesOfProcurement.CreateModeOfProcurement;

public sealed class CreateModeOfProcurementCommandHandler : ICommandHandler<CreateModeOfProcurementCommand, ModeOfProcurementDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateModeOfProcurementCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ModeOfProcurementDto> Handle(CreateModeOfProcurementCommand command, CancellationToken cancellationToken)
    {
        var nameInUse = await _dbContext.ModesOfProcurement
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Name == command.Name, cancellationToken)
            .ConfigureAwait(false);

        if (nameInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Name), "A mode of procurement with this name already exists.")
            ]);
        }

        var entity = ModeOfProcurement.Create(command.Name, command.Description);
        entity.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.ModesOfProcurement.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ModeOfProcurementDto(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.IsActive);
    }
}

