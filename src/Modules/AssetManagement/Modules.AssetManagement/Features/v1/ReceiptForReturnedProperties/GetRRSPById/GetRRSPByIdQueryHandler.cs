using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.GetRRSPById;

public sealed class GetRRSPByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetRRSPByIdQuery, RRSPDetailsDto>
{
    public async ValueTask<RRSPDetailsDto> Handle(GetRRSPByIdQuery query, CancellationToken cancellationToken)
    {
        var rrsp = await dbContext.ReceiptForReturnedProperties
            .Where(x => x.Id == query.Id)
            .Select(x => new
            {
                x.Id,
                x.RRSPNo,
                x.Date,
                x.ICSId,
                x.FundCluster,
                x.ReceivedByEmployeeId,
                x.ReturnedByEmployeeId,
                x.Remarks,
                x.CreatedOnUtc,
                x.CreatedBy,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rrsp is null)
        {
            throw new NotFoundException($"RRSP with ID {query.Id} not found.");
        }

        var icsNo = await dbContext.InventoryCustodianSlips
            .IgnoreQueryFilters()
            .Where(x => x.Id == rrsp.ICSId)
            .Select(x => x.ICSNo)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        var items = await (
            from rrspItem in dbContext.RRSPItems.Where(x => x.RRSPId == query.Id)
            join prop in dbContext.SemiExpendableProperties.IgnoreQueryFilters()
                on rrspItem.SemiExpendablePropertyId equals prop.Id
            orderby rrspItem.ItemNo
            select new RRSPItemDetailsDto(
                rrspItem.Id,
                rrspItem.SemiExpendablePropertyId,
                prop.PropertyNo,
                rrspItem.ItemNo,
                rrspItem.Description,
                rrspItem.UnitCost,
                rrspItem.CategoryAtTimeOfReturn.ToString()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new RRSPDetailsDto(
            rrsp.Id,
            rrsp.RRSPNo,
            rrsp.Date,
            rrsp.ICSId,
            icsNo,
            rrsp.FundCluster,
            rrsp.ReceivedByEmployeeId,
            rrsp.ReturnedByEmployeeId,
            rrsp.Remarks,
            rrsp.CreatedOnUtc,
            rrsp.CreatedBy,
            items);
    }
}
