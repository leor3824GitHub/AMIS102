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
        // 1. Validate ReportNo uniqueness (including soft-deleted records).
        var reportNoExists = await dbContext.UnserviceablePropertyReports
            .IgnoreQueryFilters()
            .AnyAsync(x => x.ReportNo == command.ReportNo, cancellationToken)
            .ConfigureAwait(false);

        if (reportNoExists)
        {
            throw new InvalidOperationException($"Report number '{command.ReportNo}' already exists.");
        }

        // 2. Reject duplicate item IDs in the request.
        var requestedIds = command.Items.Select(x => x.TangibleInventoryItemId).ToList();
        var distinctIds  = requestedIds.Distinct().ToList();
        if (distinctIds.Count != requestedIds.Count)
        {
            throw new InvalidOperationException("Items contains duplicate TangibleInventoryItemId entries.");
        }

        // 3. Load the TangibleInventoryItems.
        var invItems = await dbContext.TangibleInventoryItems
            .Where(x => distinctIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
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
            var invItem     = invItems[itemRequest.TangibleInventoryItemId];

            var item = UnserviceablePropertyItem.Create(
                reportId:                report.Id,
                tangibleInventoryItemId: invItem.Id,
                itemNo:                  i + 1,
                description:             invItem.Description,
                unitCost:                invItem.UnitCost,
                assetTypeAtTimeOfReport: invItem.AssetType,
                conditionRemarks:        itemRequest.ConditionRemarks);

            dbContext.UnserviceablePropertyItems.Add(item);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateUnserviceablePropertyReportResult(report.Id, report.ReportNo, orderedItems.Count);
    }
}
