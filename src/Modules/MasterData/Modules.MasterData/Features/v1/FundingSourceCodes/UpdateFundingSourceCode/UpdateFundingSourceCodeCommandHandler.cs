using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.UpdateFundingSourceCode;

public sealed class UpdateFundingSourceCodeCommandHandler : ICommandHandler<UpdateFundingSourceCodeCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateFundingSourceCodeCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdateFundingSourceCodeCommand command, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.FundingSourceCodes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new KeyNotFoundException($"Funding source code with ID {command.Id} not found.");
        }

        var codeInUse = await _dbContext.FundingSourceCodes
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code && x.Id != command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A funding source code with this code already exists.")
            ]);
        }

        var clusterExists = await _dbContext.FundClusters
            .AnyAsync(x => x.Code == command.FundClusterCode, cancellationToken)
            .ConfigureAwait(false);

        if (!clusterExists)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.FundClusterCode), "The specified fund cluster code does not exist.")
            ]);
        }

        entity.Update(
            command.Code,
            command.FundClusterCode,
            command.FinancingSource,
            command.Authorization,
            command.FundCategory,
            command.FundSubCategory,
            command.Description,
            command.DepartmentName,
            command.AgencyName,
            command.IsActive);
        entity.LastModifiedBy = _currentUser.GetUserId().ToString();

        _dbContext.FundingSourceCodes.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
