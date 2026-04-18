using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.References;
using FSH.Modules.MasterData.Data;
using FSH.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.UnitOfMeasures.CreateUnitOfMeasure;

public sealed class CreateUnitOfMeasureCommandHandler : ICommandHandler<CreateUnitOfMeasureCommand, UnitOfMeasureReferenceDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateUnitOfMeasureCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<UnitOfMeasureReferenceDto> Handle(CreateUnitOfMeasureCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await _dbContext.UnitOfMeasures
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A unit of measure with this code already exists.")
            ]);
        }

        var unitOfMeasure = UnitOfMeasure.Create(command.Code, command.Name, command.Description, command.OfficeCode);
        unitOfMeasure.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.UnitOfMeasures.Add(unitOfMeasure);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new UnitOfMeasureReferenceDto(unitOfMeasure.Id, unitOfMeasure.Code, unitOfMeasure.Name, unitOfMeasure.Description, unitOfMeasure.IsActive, unitOfMeasure.OfficeCode);
    }
}