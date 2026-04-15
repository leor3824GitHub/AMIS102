using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Reclassification.ReclassifyProperties;

public sealed class ReclassifyPropertiesCommandHandler : ICommandHandler<ReclassifyPropertiesCommand, ReclassifyPropertiesResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ReclassifyPropertiesCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ReclassifyPropertiesResult> Handle(ReclassifyPropertiesCommand command, CancellationToken cancellationToken)
    {
        var policy = await _dbContext.CapitalizationThresholdPolicies
            .FirstOrDefaultAsync(x => x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (policy is null)
        {
            throw new NotFoundException("No active capitalization threshold policy found. Set one before running reclassification.");
        }

        // Load all non-deleted properties. Query filter on IsDeleted is applied automatically.
        var properties = await _dbContext.SemiExpendableProperties
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int totalReclassified = 0;
        string userId = _currentUser.GetUserId().ToString();

        foreach (var property in properties)
        {
            var correctCategory = policy.ClassifyUnitCost(property.UnitCost);
            if (property.Category != correctCategory)
            {
                property.Reclassify(correctCategory);
                property.LastModifiedBy = userId;
                totalReclassified++;
            }
        }

        var record = ReclassificationRecord.Create(policy.Id, totalReclassified, command.Notes);
        record.CreatedBy = userId;
        _dbContext.ReclassificationRecords.Add(record);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ReclassifyPropertiesResult(record.Id, totalReclassified);
    }
}
