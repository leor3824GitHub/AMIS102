using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.CreateFundingSourceCode;

public sealed class CreateFundingSourceCodeCommandHandler : ICommandHandler<CreateFundingSourceCodeCommand, FundingSourceCodeDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateFundingSourceCodeCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<FundingSourceCodeDto> Handle(CreateFundingSourceCodeCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await _dbContext.FundingSourceCodes
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code, cancellationToken)
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

        var entity = FundingSourceCode.Create(
            command.Code,
            command.FundClusterCode,
            command.FinancingSource,
            command.Authorization,
            command.FundCategory,
            command.FundSubCategory,
            command.Description,
            command.DepartmentName,
            command.AgencyName);
        entity.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.FundingSourceCodes.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new FundingSourceCodeDto(
            entity.Id,
            entity.Code,
            entity.FundClusterCode,
            entity.FinancingSource,
            entity.Authorization,
            entity.FundCategory,
            entity.FundSubCategory,
            entity.Description,
            entity.DepartmentName,
            entity.AgencyName,
            entity.IsActive);
    }
}
