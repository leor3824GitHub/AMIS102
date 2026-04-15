using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPTR;

public sealed class GetPTRQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPTRQuery, PTRDto>
{
    public async ValueTask<PTRDto> Handle(GetPTRQuery query, CancellationToken cancellationToken)
    {
        var ppeir = await dbContext.PPEIssuanceReports
            .FirstOrDefaultAsync(x => x.Id == query.PPEIRId, cancellationToken)
            .ConfigureAwait(false);

        if (ppeir is null)
        {
            throw new KeyNotFoundException($"PPE Issuance Report with ID {query.PPEIRId} not found.");
        }

        var items = await dbContext.PPEIRItems
            .Where(x => x.PPEIRId == query.PPEIRId)
            .Join(
                dbContext.PPEItems,
                ppeirItem => ppeirItem.PPEItemId,
                ppeItem => ppeItem.Id,
                (ppeirItem, ppeItem) => new PTRItemDto(
                    ppeirItem.ItemNo,
                    ppeirItem.DateAcquired,
                    ppeItem.PropertyNumber,
                    ppeirItem.PPESpecification,
                    ppeirItem.AcquisitionCost,
                    null,
                    null))
            .OrderBy(x => x.ItemNo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PTRDto(
            PTRNo:                   ppeir.PPEIRNo,
            Date:                    ppeir.Date,
            FromAccountableOfficerId: ppeir.IssuedByEmployeeId,
            ToAccountableOfficerId:  ppeir.IssuedToEmployeeId,
            ToOfficeAddress:         ppeir.IssuedToOfficeAddress,
            TransferType:            ppeir.IssuanceType.ToString(),
            ApprovedByEmployeeId:    ppeir.ApprovedByEmployeeId,
            ReleasedByEmployeeId:    ppeir.IssuedByEmployeeId,
            ReceivedByEmployeeId:    ppeir.ReceivedByEmployeeId,
            Items:                   items);
    }
}
