using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.CreateUnserviceablePropertyReport;

public sealed class CreateUnserviceablePropertyReportCommandHandler(AssetManagementDbContext dbContext)
    : ICommandHandler<CreateUnserviceablePropertyReportCommand, CreateUnserviceablePropertyReportResult>
{
    private static readonly PropertyStatus[] EligibleStatuses =
    [
        PropertyStatus.OnHand,
        PropertyStatus.Returned,
        PropertyStatus.LostStolenDamaged,
    ];

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

        // 2. Reject duplicate property IDs in the request.
        var requestedIds = command.Items.Select(x => x.SemiExpendablePropertyId).ToList();
        var distinctIds  = requestedIds.Distinct().ToList();
        if (distinctIds.Count != requestedIds.Count)
        {
            throw new InvalidOperationException("Items contains duplicate SemiExpendablePropertyId entries.");
        }

        // 3. Load the properties with their catalog item name (used as line-item description).
        var properties = await dbContext.SemiExpendableProperties
            .Include(x => x.Item)
            .Where(x => distinctIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        var missingIds = distinctIds.Except(properties.Keys).ToList();
        if (missingIds.Count > 0)
        {
            throw new NotFoundException(
                $"SemiExpendableProperty IDs not found: {string.Join(", ", missingIds)}.");
        }

        // 4. Validate each property is in an eligible status.
        var blocked = properties.Values
            .Where(x => !EligibleStatuses.Contains(x.Status))
            .Select(x => $"{x.PropertyNo} ({x.Status})")
            .ToList();

        if (blocked.Count > 0)
        {
            throw new InvalidOperationException(
                "The following properties are not eligible for disposal " +
                "(must be OnHand, Returned, or LostStolenDamaged): " +
                string.Join(", ", blocked));
        }

        // 5. Create the report header.
        var report = UnserviceablePropertyReport.Create(
            command.ReportNo,
            command.Date,
            command.DisposalMethod,
            command.FundCluster,
            command.InspectedByEmployeeId,
            command.ApprovedByEmployeeId,
            command.Remarks);

        dbContext.UnserviceablePropertyReports.Add(report);

        // 6. Create items ordered by PropertyNo (consistent numbering), then mark disposed.
        var orderedItems = command.Items
            .OrderBy(x => properties[x.SemiExpendablePropertyId].PropertyNo)
            .ToList();

        for (var i = 0; i < orderedItems.Count; i++)
        {
            var itemRequest = orderedItems[i];
            var property    = properties[itemRequest.SemiExpendablePropertyId];

            var item = UnserviceablePropertyItem.Create(
                reportId:                 report.Id,
                semiExpendablePropertyId: property.Id,
                itemNo:                   i + 1,
                description:              property.Item.Name,
                unitCost:                 property.UnitCost,
                categoryAtTimeOfReport:   property.Category,
                conditionRemarks:         itemRequest.ConditionRemarks);

            dbContext.UnserviceablePropertyItems.Add(item);

            property.SetStatus(PropertyStatus.Disposed, custodianId: null);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateUnserviceablePropertyReportResult(report.Id, report.ReportNo, orderedItems.Count);
    }
}
