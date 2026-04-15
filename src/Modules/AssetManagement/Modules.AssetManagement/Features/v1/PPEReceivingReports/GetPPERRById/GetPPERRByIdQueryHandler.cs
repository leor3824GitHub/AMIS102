using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.GetPPERRById;

public sealed class GetPPERRByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPPERRByIdQuery, PPERRDetailsDto>
{
    public async ValueTask<PPERRDetailsDto> Handle(GetPPERRByIdQuery query, CancellationToken cancellationToken)
    {
        var pperr = await dbContext.PPEReceivingReports
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (pperr is null)
        {
            throw new KeyNotFoundException($"PPE Receiving Report with ID {query.Id} not found.");
        }

        var items = await dbContext.PPERRItems
            .Where(x => x.PPERRId == query.Id)
            .OrderBy(x => x.ItemNo)
            .Select(x => new PPERRItemDto(
                x.Id,
                x.ItemNo,
                x.PropertyCode,
                x.Description,
                x.DateAcquired,
                x.Quantity,
                x.UnitCost,
                x.Amount))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PPERRDetailsDto(
            pperr.Id,
            pperr.PPERRNo,
            pperr.Date,
            pperr.ReceivedFrom,
            pperr.Address,
            pperr.ReceiptNature.ToString(),
            pperr.ReceivedByEmployeeId,
            pperr.NotedByEmployeeId,
            pperr.CreatedOnUtc,
            pperr.CreatedBy,
            items);
    }
}
