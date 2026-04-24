using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPPEIRById;

public sealed class GetPPEIRByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPPEIRByIdQuery, PPEIRDetailsDto>
{
    public async ValueTask<PPEIRDetailsDto> Handle(GetPPEIRByIdQuery query, CancellationToken cancellationToken)
    {
        var ppeir = await dbContext.PPEIssuanceReports
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (ppeir is null)
        {
            throw new KeyNotFoundException($"PPE Issuance Report with ID {query.Id} not found.");
        }

        var items = await dbContext.PPEIRItems
            .Where(x => x.PPEIRId == query.Id)
            .OrderBy(x => x.ItemNo)
            .Select(x => new PPEIRItemDto(
                x.Id,
                x.ItemNo,
                x.TangibleInventoryItemId,
                x.PropertyCode,
                x.SerialNumber,
                x.PPESpecification,
                x.DateAcquired,
                x.AcquisitionCost,
                x.AccumulatedDepreciation,
                x.BookValue))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PPEIRDetailsDto(
            ppeir.Id,
            ppeir.PPEIRNo,
            ppeir.Date,
            ppeir.IssuedToEmployeeId,
            ppeir.IssuedToOfficeAddress,
            ppeir.IssuanceType.ToString(),
            ppeir.IssuedByEmployeeId,
            ppeir.ReceivedByEmployeeId,
            ppeir.DateReceived,
            ppeir.ApprovedByEmployeeId,
            ppeir.DriverName,
            ppeir.BillOfLadingNo,
            ppeir.CreatedOnUtc,
            ppeir.CreatedBy,
            items);
    }
}
