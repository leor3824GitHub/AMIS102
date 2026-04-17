using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.References;
using FSH.Modules.MasterData.Data;
using FSH.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.Offices.CreateOffice;

public sealed class CreateOfficeCommandHandler : ICommandHandler<CreateOfficeCommand, OfficeReferenceDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateOfficeCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<OfficeReferenceDto> Handle(CreateOfficeCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await _dbContext.Offices
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "An office with this code already exists.")
            ]);
        }

        var office = Office.Create(command.Code, command.Name, command.Description, command.RegProvCode, command.LocationCode, command.Address);
        office.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.Offices.Add(office);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new OfficeReferenceDto(office.Id, office.Code, office.Name, office.Description, office.Address, office.LocationCode, office.RegProvCode, office.IsActive);
    }
}
