using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.GetPurchaseRequest;

public sealed class GetPurchaseRequestQueryHandler(ProcurementDbContext dbContext, IMediator mediator)
    : IQueryHandler<GetPurchaseRequestQuery, PurchaseRequestDto?>
{
    public async ValueTask<PurchaseRequestDto?> Handle(GetPurchaseRequestQuery query, CancellationToken cancellationToken)
    {
        var pr = await dbContext.PurchaseRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (pr is null) return null;

        var department = await mediator.Send(new GetDepartmentReferenceByIdQuery(pr.DepartmentId), cancellationToken)
            .ConfigureAwait(false);

        return new PurchaseRequestDto(
            pr.Id,
            pr.PrNumber,
            pr.PrDate,
            pr.SaiNumber,
            pr.SaiDate,
            pr.AlobsNumber,
            pr.AlobsDate,
            pr.DepartmentId,
            department?.Name ?? string.Empty,
            pr.ResponsibilityCenterCode,
            pr.Purpose,
            pr.PrType,
            pr.Justification,
            pr.Status,
            pr.RequestedByName,
            pr.ApprovedByName,
            pr.LineItems.Select(li => new PurchaseRequestLineItemDto(
                li.ItemNo, li.Quantity, li.UnitOfIssue, li.ItemDescription,
                li.EstimatedUnitCost, li.EstimatedTotalCost)).ToList(),
            pr.CreatedOnUtc,
            pr.CreatedBy,
            pr.LastModifiedOnUtc);
    }
}

