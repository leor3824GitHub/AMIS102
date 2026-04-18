using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.GetSMIRById;

public sealed class GetSMIRByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetSMIRByIdQuery, SMIRDetailsDto>
{
    public async ValueTask<SMIRDetailsDto> Handle(GetSMIRByIdQuery query, CancellationToken cancellationToken)
    {
        var smir = await dbContext.SemiExpendableIssuanceRecords
            .Where(x => x.Id == query.Id)
            .Select(x => new
            {
                x.Id,
                x.SMIRNo,
                x.Date,
                x.FundCluster,
                x.IssuanceType,
                x.TransferredToTenantId,
                x.TransferredToOfficerName,
                x.IssuedByEmployeeId,
                x.Remarks,
                x.CreatedOnUtc,
                x.CreatedBy,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (smir is null)
        {
            throw new NotFoundException($"SMIR with ID {query.Id} not found.");
        }

        var items = await (
            from smirItem in dbContext.SMIRItems.Where(x => x.SMIRId == query.Id)
            join inv in dbContext.TangibleInventoryItems.IgnoreQueryFilters()
                on smirItem.TangibleInventoryItemId equals inv.Id
            orderby smirItem.ItemNo
            select new SMIRItemDetailsDto(
                smirItem.Id,
                smirItem.TangibleInventoryItemId,
                inv.PropertyNo,
                smirItem.ItemNo,
                smirItem.Description,
                smirItem.UnitCost,
                smirItem.AssetTypeAtTimeOfIssuance.ToString()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new SMIRDetailsDto(
            smir.Id,
            smir.SMIRNo,
            smir.Date,
            smir.FundCluster,
            smir.IssuanceType.ToString(),
            smir.TransferredToTenantId,
            smir.TransferredToOfficerName,
            smir.IssuedByEmployeeId,
            smir.Remarks,
            smir.CreatedOnUtc,
            smir.CreatedBy,
            items);
    }
}
