using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.GetUnserviceablePropertyReportById;

public sealed class GetUnserviceablePropertyReportByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetUnserviceablePropertyReportByIdQuery, UnserviceablePropertyReportDetailsDto>
{
    public async ValueTask<UnserviceablePropertyReportDetailsDto> Handle(
        GetUnserviceablePropertyReportByIdQuery query,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.UnserviceablePropertyReports
            .Where(x => x.Id == query.Id)
            .Select(x => new
            {
                x.Id,
                x.ReportNo,
                x.Date,
                x.DisposalMethod,
                x.FundCluster,
                x.InspectedByEmployeeId,
                x.ApprovedByEmployeeId,
                x.Remarks,
                x.CreatedOnUtc,
                x.CreatedBy,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (report is null)
        {
            throw new NotFoundException($"Unserviceable Property Report with ID {query.Id} not found.");
        }

        var items = await (
            from item in dbContext.UnserviceablePropertyItems.Where(x => x.ReportId == query.Id)
            join prop in dbContext.SemiExpendableProperties.IgnoreQueryFilters()
                on item.SemiExpendablePropertyId equals prop.Id
            orderby item.ItemNo
            select new UnserviceablePropertyItemDetailsDto(
                item.Id,
                item.SemiExpendablePropertyId,
                prop.PropertyNo,
                item.ItemNo,
                item.Description,
                item.UnitCost,
                item.CategoryAtTimeOfReport.ToString(),
                item.ConditionRemarks))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new UnserviceablePropertyReportDetailsDto(
            report.Id,
            report.ReportNo,
            report.Date,
            report.DisposalMethod.ToString(),
            report.FundCluster,
            report.InspectedByEmployeeId,
            report.ApprovedByEmployeeId,
            report.Remarks,
            report.CreatedOnUtc,
            report.CreatedBy,
            items);
    }
}
