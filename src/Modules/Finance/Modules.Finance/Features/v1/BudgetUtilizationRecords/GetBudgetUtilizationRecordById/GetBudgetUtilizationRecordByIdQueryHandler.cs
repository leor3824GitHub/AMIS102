using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.GetBudgetUtilizationRecordById;

public sealed class GetBudgetUtilizationRecordByIdQueryHandler(
    FinanceDbContext dbContext) : IQueryHandler<GetBudgetUtilizationRecordByIdQuery, BudgetUtilizationRecordDto>
{
    public async ValueTask<BudgetUtilizationRecordDto> Handle(GetBudgetUtilizationRecordByIdQuery query, CancellationToken cancellationToken)
    {
        var bur = await dbContext.BudgetUtilizationRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Budget utilization record '{query.Id}' not found.");

        return new BudgetUtilizationRecordDto(
            bur.Id,
            bur.BurNumber,
            bur.BurDate,
            bur.PurchaseOrderId,
            bur.PurchaseOrderNumber,
            bur.DisbursementVoucherId,
            bur.DisbursementVoucherNumber,
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

