using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FSH.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.GetPurchaseRequest;

public sealed class GetPurchaseRequestQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<GetPurchaseRequestQuery, PurchaseRequestDto?>
{
    public async ValueTask<PurchaseRequestDto?> Handle(GetPurchaseRequestQuery query, CancellationToken cancellationToken)
    {
        var pr = await dbContext.PurchaseRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (pr is null) return null;

        return new PurchaseRequestDto(
            pr.Id,
            pr.PrNumber,
            pr.PrDate,
            pr.SaiNumber,
            pr.SaiDate,
            pr.AlobsNumber,
            pr.AlobsDate,
            pr.DepartmentId,
            string.Empty,
            pr.Section,
            pr.Purpose,
            pr.PrType,
            pr.Justification,
            pr.Status,
            pr.RequestedById,
            string.Empty,
            pr.ApprovedById,
            null,
            pr.LineItems.Select(li => new PurchaseRequestLineItemDto(
                li.ItemNo, li.Quantity, li.UnitOfIssue, li.ItemDescription,
                li.EstimatedUnitCost, li.EstimatedTotalCost)).ToList(),
            pr.CreatedOnUtc,
            pr.CreatedBy,
            pr.LastModifiedOnUtc);
    }
}
