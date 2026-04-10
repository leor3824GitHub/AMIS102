using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.GetSMRRById;

public sealed class GetSMRRByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetSMRRByIdQuery, SMRRDetailsDto>
{
    public async ValueTask<SMRRDetailsDto> Handle(GetSMRRByIdQuery query, CancellationToken cancellationToken)
    {
        var smrr = await dbContext.SuppliesMaterialsReceivingReports
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (smrr is null)
        {
            throw new KeyNotFoundException($"Receiving report with ID {query.Id} not found.");
        }

        var items = await dbContext.SMRRItems
            .Where(x => x.SMRRId == query.Id)
            .Join(
                dbContext.SemiExpendableItems,
                item => item.SemiExpendableItemId,
                catalogItem => catalogItem.Id,
                (item, catalogItem) => new SMRRItemDetailsDto(
                    item.Id,
                    item.Reference,
                    item.SemiExpendableItemId,
                    catalogItem.Code,
                    catalogItem.Name,
                    item.Description,
                    item.AcquisitionDate,
                    item.Quantity,
                    item.UnitCost,
                    item.Amount))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new SMRRDetailsDto(
            smrr.Id,
            smrr.SMRRNo,
            smrr.Date,
            smrr.ReceivedFrom,
            smrr.Address,
            smrr.ReceiptType.ToString(),
            smrr.OtherReceiptType,
            smrr.FundCluster,
            smrr.ReceivedBy,
            smrr.NotedBy,
            smrr.CreatedOnUtc,
            smrr.CreatedBy,
            items);
    }
}
