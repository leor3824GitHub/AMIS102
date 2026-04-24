using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using FSH.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Reclassification.ReclassifyProperties;

public sealed class ReclassifyPropertiesCommandHandler : ICommandHandler<ReclassifyPropertiesCommand, ReclassifyPropertiesResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public ReclassifyPropertiesCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser, IMediator mediator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async ValueTask<ReclassifyPropertiesResult> Handle(ReclassifyPropertiesCommand command, CancellationToken cancellationToken)
    {
        var threshold = await _mediator.Send(new GetActiveCapitalizationThresholdQuery(), cancellationToken)
            .ConfigureAwait(false);

        if (threshold is null)
        {
            throw new NotFoundException("No active capitalization threshold found. Set one in Master Data → Capitalization Thresholds before running reclassification.");
        }

        // Load all non-deleted tangible inventory items. Query filter on IsDeleted is applied automatically.
        var invItems = await _dbContext.TangibleInventoryItems
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int totalReclassified = 0;
        string tenantId = _currentUser.GetTenant() ?? string.Empty;
        string userId = _currentUser.GetUserId().ToString();

        foreach (var invItem in invItems)
        {
            var correctAssetType = invItem.UnitCost < threshold.CapitalizationAmount
                ? AssetType.SE
                : AssetType.PPE;

            if (invItem.AssetType != correctAssetType || invItem.ThresholdAmountUsed != threshold.CapitalizationAmount)
            {
                invItem.Reclassify(correctAssetType, threshold.CapitalizationAmount);
                totalReclassified++;
            }
        }

        var record = ReclassificationRecord.Create(tenantId, threshold.Id, totalReclassified, command.Notes);
        record.CreatedBy = userId;
        _dbContext.ReclassificationRecords.Add(record);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ReclassifyPropertiesResult(record.Id, totalReclassified);
    }
}
