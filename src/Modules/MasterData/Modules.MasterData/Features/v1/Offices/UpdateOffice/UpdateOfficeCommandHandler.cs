using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.References;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.Offices.UpdateOffice;

public sealed class UpdateOfficeCommandHandler : ICommandHandler<UpdateOfficeCommand, OfficeReferenceDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateOfficeCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<OfficeReferenceDto> Handle(UpdateOfficeCommand command, CancellationToken cancellationToken)
    {
        var office = await _dbContext.Offices
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Office {command.Id} not found.");

        var codeInUse = await _dbContext.Offices
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != command.Id && x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "An office with this code already exists.")
            ]);
        }

        office.Update(command.Code, command.Name, command.Description, command.IsActive, command.RegProvCode, command.LocationCode, command.Address);
        office.LastModifiedBy = _currentUser.GetUserId().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new OfficeReferenceDto(office.Id, office.Code, office.Name, office.Description, office.Address, office.LocationCode, office.RegProvCode, office.IsActive, office.OfficeCode);
    }
}
