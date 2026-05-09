using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.CreateUnserviceablePropertyReport;

public sealed class CreateUnserviceablePropertyReportCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreateUnserviceablePropertyReportCommand, CreateUnserviceablePropertyReportResult>
{
    public async ValueTask<CreateUnserviceablePropertyReportResult> Handle(
        CreateUnserviceablePropertyReportCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Validate ReportNo uniqueness within this tenant.
        var reportNoExists = await dbContext.UnserviceablePropertyReports
            .AnyAsync(x => x.ReportNo == command.ReportNo, cancellationToken)
            .ConfigureAwait(false);

        if (reportNoExists)
        {
            throw new InvalidOperationException($"Report number '{command.ReportNo}' already exists.");
        }

        // 2. Reject duplicate item IDs in the request.
        var requestedIds = command.Items.Select(x => x.TangibleInventoryItemId).ToList();
        var distinctIds = requestedIds.Distinct().ToList();
        if (distinctIds.Count != requestedIds.Count)
        {
            throw new InvalidOperationException("Items contains duplicate TangibleInventoryItemId entries.");
        }

        // 3. Load the TangibleInventoryItems.
        var invItems = await dbContext.TangibleInventoryItems
            .Where(x => distinctIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        var registryByInventoryItemId = await dbContext.AssetRegistry
            .Where(x => distinctIds.Contains(x.TangibleInventoryItemId))
            .ToDictionaryAsync(x => x.TangibleInventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        var missingIds = distinctIds.Except(invItems.Keys).ToList();
        if (missingIds.Count > 0)
        {
            throw new NotFoundException(
                $"TangibleInventoryItem IDs not found: {string.Join(", ", missingIds)}.");
        }

        // 4. Create the report header.
        string tenantId = currentUser.GetTenant() ?? string.Empty;

        var report = UnserviceablePropertyReport.Create(
            tenantId,
            command.ReportNo,
            command.Date,
            command.DisposalMethod,
            command.FundCluster,
            command.InspectedByEmployeeId,
            command.ApprovedByEmployeeId,
            command.Remarks);

        dbContext.UnserviceablePropertyReports.Add(report);

        // 5. Create items ordered by PropertyNo (consistent numbering).
        var orderedItems = command.Items
            .OrderBy(x => invItems[x.TangibleInventoryItemId].PropertyNo)
            .ToList();

        for (var i = 0; i < orderedItems.Count; i++)
        {
            var itemRequest = orderedItems[i];
            var invItem = invItems[itemRequest.TangibleInventoryItemId];

            var item = UnserviceablePropertyItem.Create(
                tenantId: tenantId,
                reportId: report.Id,
                tangibleInventoryItemId: invItem.Id,
                itemNo: i + 1,
                description: invItem.Description,
                unitCost: invItem.UnitCost,
                assetTypeAtTimeOfReport: invItem.AssetType,
                conditionRemarks: itemRequest.ConditionRemarks);

            dbContext.UnserviceablePropertyItems.Add(item);

            if (!registryByInventoryItemId.TryGetValue(invItem.Id, out var registry))
            {
                registry = AssetRegistry.Create(
                    tenantId: invItem.TenantId,
                    tangibleInventoryItemId: invItem.Id,
                    itemId: invItem.ItemId,
                    propertyNo: invItem.PropertyNo,
                    assetType: invItem.AssetType,
                    acquisitionDate: invItem.AcquisitionDate,
                    unitCost: invItem.UnitCost);

                dbContext.AssetRegistry.Add(registry);
                registryByInventoryItemId[invItem.Id] = registry;
            }

            var previousCustodian = registry.CurrentCustodianId;
            registry.MarkDisposed();

            var history = AssetAssignmentHistory.Create(
                tenantId,
                registry.Id,
                AssetAssignmentEventType.StatusChanged,
                DateTimeOffset.UtcNow,
                "IIRUSP",
                report.Id,
                report.ReportNo,
                previousCustodian,
                null,
                registry.CurrentLocationId,
                itemRequest.ConditionRemarks);

            dbContext.AssetAssignmentHistory.Add(history);
            registry.LinkCurrentAssignment(history.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateUnserviceablePropertyReportResult(report.Id, report.ReportNo, orderedItems.Count);
    }
}
