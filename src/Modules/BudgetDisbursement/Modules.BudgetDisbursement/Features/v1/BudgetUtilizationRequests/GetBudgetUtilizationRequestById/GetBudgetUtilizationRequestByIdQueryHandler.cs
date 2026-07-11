using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.GetBudgetUtilizationRequestById;

public sealed class GetBudgetUtilizationRequestByIdQueryHandler(
    BudgetDisbursementDbContext dbContext) : IQueryHandler<GetBudgetUtilizationRequestByIdQuery, BudgetUtilizationRequestDto>
{
    public async ValueTask<BudgetUtilizationRequestDto> Handle(GetBudgetUtilizationRequestByIdQuery query, CancellationToken cancellationToken)
    {
        var bur = await dbContext.BudgetUtilizationRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Budget utilization record '{query.Id}' not found.");

        return new BudgetUtilizationRequestDto(
            bur.Id,
            bur.BurNumber,
            bur.BurDate,
            bur.PurchaseOrderId,
            bur.PurchaseOrderNumber,
            bur.DisbursementVoucherId,
            bur.DisbursementVoucherNumber,
            bur.FundCluster,
            bur.AllotmentClass,
            bur.UacsObjectCode,
            bur.ResponsibilityCenter,
            bur.Particulars,
            bur.Amount,
            bur.Status,
            bur.Remarks,
            bur.CreatedOnUtc.DateTime,
            bur.LastModifiedOnUtc?.DateTime);
    }
}

