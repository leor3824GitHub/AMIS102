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

        var invItemIds = invItems.Select(x => x.Id).ToList();
        var registryByInventoryItemId = await _dbContext.AssetRegistry
            .Where(x => invItemIds.Contains(x.TangibleInventoryItemId))
            .ToDictionaryAsync(x => x.TangibleInventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        int totalReclassified = 0;
        var reclassifiedItemIds = new HashSet<Guid>();
        string tenantId = _currentUser.GetTenant() ?? string.Empty;
        string userId = _currentUser.GetUserId().ToString();

        foreach (var invItem in invItems)
        {
            var correctAssetType = invItem.UnitCost < threshold.CapitalizationAmount
                ? AssetType.SE
                : AssetType.PPE;

            if (invItem.AssetType != correctAssetType || invItem.ThresholdAmountUsed != threshold.CapitalizationAmount)
            {
                var previousAssetType = invItem.AssetType;
                invItem.Reclassify(correctAssetType, threshold.CapitalizationAmount);

                if (!registryByInventoryItemId.TryGetValue(invItem.Id, out var registry))
                {
                    // Snapshot the pre-change classification first, then apply the new
                    // threshold-derived classification below. This preserves transition
                    // semantics for first-time registry materialization.
                    registry = AssetRegistry.Create(
                        tenantId: invItem.TenantId,
                        tangibleInventoryItemId: invItem.Id,
                        itemId: invItem.ItemId,
                        propertyNo: invItem.PropertyNo,
                        assetType: previousAssetType,
                        acquisitionDate: invItem.AcquisitionDate,
                        unitCost: invItem.UnitCost);

                    _dbContext.AssetRegistry.Add(registry);
                    registryByInventoryItemId[invItem.Id] = registry;
                }

                registry.ReclassifyAssetType(correctAssetType);
                totalReclassified++;
                reclassifiedItemIds.Add(invItem.Id);
            }
        }

        var record = ReclassificationRecord.Create(tenantId, threshold.Id, totalReclassified, command.Notes);
        record.CreatedBy = userId;
        _dbContext.ReclassificationRecords.Add(record);

        foreach (var invItem in invItems)
        {
            if (!reclassifiedItemIds.Contains(invItem.Id))
            {
                continue;
            }

            if (!registryByInventoryItemId.TryGetValue(invItem.Id, out var registry))
            {
                continue;
            }

            var history = AssetAssignmentHistory.Create(
                tenantId,
                registry.Id,
                AssetAssignmentEventType.StatusChanged,
                DateTimeOffset.UtcNow,
                "RECLASSIFICATION",
                record.Id,
                record.Id.ToString(),
                registry.CurrentCustodianId,
                registry.CurrentCustodianId,
                registry.CurrentLocationId,
                command.Notes);

            _dbContext.AssetAssignmentHistory.Add(history);
            registry.LinkCurrentAssignment(history.Id);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ReclassifyPropertiesResult(record.Id, totalReclassified);
    }
}
