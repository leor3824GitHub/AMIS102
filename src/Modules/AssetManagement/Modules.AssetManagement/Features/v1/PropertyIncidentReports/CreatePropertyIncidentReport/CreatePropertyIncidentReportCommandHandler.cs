using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.PropertyIncidentReports.CreatePropertyIncidentReport;

public sealed class CreatePropertyIncidentReportCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreatePropertyIncidentReportCommand, CreatePropertyIncidentReportResult>
{
    public async ValueTask<CreatePropertyIncidentReportResult> Handle(
        CreatePropertyIncidentReportCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Validate ReportNo uniqueness within this tenant.
        var reportNoExists = await dbContext.PropertyIncidentReports
            .AnyAsync(x => x.ReportNo == command.ReportNo, cancellationToken)
            .ConfigureAwait(false);

        if (reportNoExists)
        {
            throw new InvalidOperationException($"Report number '{command.ReportNo}' already exists.");
        }

        // 2. Reject duplicate item IDs in the request.
        var distinctIds = command.TangibleInventoryItemIds.Distinct().ToList();
        if (distinctIds.Count != command.TangibleInventoryItemIds.Count)
        {
            throw new InvalidOperationException("TangibleInventoryItemIds contains duplicate entries.");
        }

        // 3. Load the TangibleInventoryItems.
        var invItems = await dbContext.TangibleInventoryItems
            .Where(x => distinctIds.Contains(x.Id))
            .OrderBy(x => x.PropertyNo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var registryByInventoryItemId = await dbContext.AssetRegistry
            .Where(x => distinctIds.Contains(x.TangibleInventoryItemId))
            .ToDictionaryAsync(x => x.TangibleInventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        var missingIds = distinctIds.Except(invItems.Select(x => x.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new NotFoundException(
                $"TangibleInventoryItem IDs not found: {string.Join(", ", missingIds)}.");
        }

        // 4. Create the report header.
        string tenantId = currentUser.GetTenant() ?? string.Empty;

        var report = PropertyIncidentReport.Create(
            tenantId,
            command.ReportNo,
            command.Date,
            command.IncidentDate,
            command.IncidentType,
            command.FundCluster,
            command.AccountableEmployeeId,
            command.IncidentDetails,
            command.Remarks);

        dbContext.PropertyIncidentReports.Add(report);

        // 5. Snapshot each inventory item into an incident line.
        for (var i = 0; i < invItems.Count; i++)
        {
            var invItem = invItems[i];

            var item = PropertyIncidentItem.Create(
                tenantId: tenantId,
                reportId: report.Id,
                tangibleInventoryItemId: invItem.Id,
                itemNo: i + 1,
                description: invItem.Description,
                unitCost: invItem.UnitCost,
                assetTypeAtTimeOfReport: invItem.AssetType);

            dbContext.PropertyIncidentItems.Add(item);

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

            var currentCustodian = registry.CurrentCustodianId;
            registry.MarkUnderInvestigation();

            var history = AssetAssignmentHistory.Create(
                tenantId,
                registry.Id,
                AssetAssignmentEventType.StatusChanged,
                DateTimeOffset.UtcNow,
                "PIR",
                report.Id,
                report.ReportNo,
                currentCustodian,
                currentCustodian,
                registry.CurrentLocationId,
                command.Remarks);

            dbContext.AssetAssignmentHistory.Add(history);
            registry.LinkCurrentAssignment(history.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreatePropertyIncidentReportResult(report.Id, report.ReportNo, invItems.Count);
    }
}

