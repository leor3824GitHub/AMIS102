using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.CreatePropertyIncidentReport;

public sealed class CreatePropertyIncidentReportCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreatePropertyIncidentReportCommand, CreatePropertyIncidentReportResult>
{
    private static readonly PropertyStatus[] TerminalStatuses =
    [
        PropertyStatus.Transferred,
        PropertyStatus.Disposed,
        PropertyStatus.LostStolenDamaged,
    ];

    public async ValueTask<CreatePropertyIncidentReportResult> Handle(
        CreatePropertyIncidentReportCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Validate ReportNo uniqueness (including soft-deleted records).
        var reportNoExists = await dbContext.PropertyIncidentReports
            .IgnoreQueryFilters()
            .AnyAsync(x => x.ReportNo == command.ReportNo, cancellationToken)
            .ConfigureAwait(false);

        if (reportNoExists)
        {
            throw new InvalidOperationException($"Report number '{command.ReportNo}' already exists.");
        }

        // 2. Reject duplicate property IDs in the request.
        var distinctIds = command.PropertyIds.Distinct().ToList();
        if (distinctIds.Count != command.PropertyIds.Count)
        {
            throw new InvalidOperationException("PropertyIds contains duplicate entries.");
        }

        // 3. Load the properties with their catalog item name (used as line-item description).
        var properties = await dbContext.SemiExpendableProperties
            .Include(x => x.Item)
            .Where(x => distinctIds.Contains(x.Id))
            .OrderBy(x => x.PropertyNo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var missingIds = distinctIds.Except(properties.Select(x => x.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new NotFoundException(
                $"SemiExpendableProperty IDs not found: {string.Join(", ", missingIds)}.");
        }

        // 4. Validate each property is not already in a terminal state.
        var blocked = properties
            .Where(x => TerminalStatuses.Contains(x.Status))
            .Select(x => $"{x.PropertyNo} ({x.Status})")
            .ToList();

        if (blocked.Count > 0)
        {
            throw new InvalidOperationException(
                $"The following properties cannot be reported (already in a terminal state): " +
                string.Join(", ", blocked));
        }

        // 5. Create the report header.
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

        // 6. Create items and mark each property as lost/stolen/damaged/destroyed.
        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];

            var item = PropertyIncidentItem.Create(
                reportId:                 report.Id,
                semiExpendablePropertyId: property.Id,
                itemNo:                   i + 1,
                description:              property.Item.Name,
                unitCost:                 property.UnitCost,
                categoryAtTimeOfReport:   property.Category);

            dbContext.PropertyIncidentItems.Add(item);

            // Clear custodian — the employee is still accountable via ICS,
            // but the property is no longer physically in their possession.
            property.SetStatus(PropertyStatus.LostStolenDamaged, custodianId: null);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreatePropertyIncidentReportResult(report.Id, report.ReportNo, properties.Count);
    }
}
