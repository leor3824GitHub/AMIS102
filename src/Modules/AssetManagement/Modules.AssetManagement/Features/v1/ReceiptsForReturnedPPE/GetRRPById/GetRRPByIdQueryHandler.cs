using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.GetRRPById;

public sealed class GetRRPByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetRRPByIdQuery, RRPDetailsDto>
{
    public async ValueTask<RRPDetailsDto> Handle(GetRRPByIdQuery query, CancellationToken cancellationToken)
    {
        var rrp = await dbContext.ReceiptsForReturnedPPE
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (rrp is null)
        {
            throw new KeyNotFoundException($"Receipt for Returned PPE with ID {query.Id} not found.");
        }

        var items = await dbContext.RRPItems
            .Where(x => x.RRPId == query.Id)
            .OrderBy(x => x.ItemNo)
            .Select(x => new RRPItemDto(
                x.Id,
                x.ItemNo,
                x.TangibleInventoryItemId,
                x.SourceDocumentRef,
                x.PropertyCode,
                x.Description,
                x.Quantity,
                x.UnitCost,
                x.TotalCost))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new RRPDetailsDto(
            rrp.Id,
            rrp.RRPNo,
            rrp.Date,
            rrp.ReturnCategory.ToString(),
            rrp.ReturnedByEmployeeId,
            rrp.ApprovedByEmployeeId,
            rrp.SignedByEmployeeId,
            rrp.PropertyInspectorCertified,
            rrp.CreatedOnUtc,
            rrp.CreatedBy,
            items);
    }
}

