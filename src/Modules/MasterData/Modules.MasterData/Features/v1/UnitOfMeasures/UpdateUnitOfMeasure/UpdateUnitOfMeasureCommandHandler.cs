using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.References;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.UnitOfMeasures.UpdateUnitOfMeasure;

public sealed class UpdateUnitOfMeasureCommandHandler : ICommandHandler<UpdateUnitOfMeasureCommand, UnitOfMeasureReferenceDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateUnitOfMeasureCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<UnitOfMeasureReferenceDto> Handle(UpdateUnitOfMeasureCommand command, CancellationToken cancellationToken)
    {
        var unitOfMeasure = await _dbContext.UnitOfMeasures
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"UnitOfMeasure {command.Id} not found.");

        var codeInUse = await _dbContext.UnitOfMeasures
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != command.Id && x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A unit of measure with this code already exists.")
            ]);
        }

        unitOfMeasure.Update(command.Code, command.Name, command.Description, command.IsActive);
        unitOfMeasure.LastModifiedBy = _currentUser.GetUserId().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new UnitOfMeasureReferenceDto(unitOfMeasure.Id, unitOfMeasure.Code, unitOfMeasure.Name, unitOfMeasure.Description, unitOfMeasure.IsActive, unitOfMeasure.OfficeCode);
    }
}