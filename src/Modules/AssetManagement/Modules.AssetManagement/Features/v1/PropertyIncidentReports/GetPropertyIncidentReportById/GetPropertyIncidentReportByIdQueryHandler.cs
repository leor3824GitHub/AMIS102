using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.GetPropertyIncidentReportById;

public sealed class GetPropertyIncidentReportByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPropertyIncidentReportByIdQuery, PropertyIncidentReportDetailsDto>
{
    public async ValueTask<PropertyIncidentReportDetailsDto> Handle(
        GetPropertyIncidentReportByIdQuery query,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.PropertyIncidentReports
            .Where(x => x.Id == query.Id)
            .Select(x => new
            {
                x.Id,
                x.ReportNo,
                x.Date,
                x.IncidentDate,
                x.IncidentType,
                x.FundCluster,
                x.AccountableEmployeeId,
                x.IncidentDetails,
                x.Remarks,
                x.CreatedOnUtc,
                x.CreatedBy,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (report is null)
        {
            throw new NotFoundException($"Property Incident Report with ID {query.Id} not found.");
        }

        var items = await (
            from item in dbContext.PropertyIncidentItems.Where(x => x.ReportId == query.Id)
            join prop in dbContext.SemiExpendableProperties.IgnoreQueryFilters()
                on item.SemiExpendablePropertyId equals prop.Id
            orderby item.ItemNo
            select new PropertyIncidentItemDetailsDto(
                item.Id,
                item.SemiExpendablePropertyId,
                prop.PropertyNo,
                item.ItemNo,
                item.Description,
                item.UnitCost,
                item.CategoryAtTimeOfReport.ToString()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PropertyIncidentReportDetailsDto(
            report.Id,
            report.ReportNo,
            report.Date,
            report.IncidentDate,
            report.IncidentType.ToString(),
            report.FundCluster,
            report.AccountableEmployeeId,
            report.IncidentDetails,
            report.Remarks,
            report.CreatedOnUtc,
            report.CreatedBy,
            items);
    }
}
