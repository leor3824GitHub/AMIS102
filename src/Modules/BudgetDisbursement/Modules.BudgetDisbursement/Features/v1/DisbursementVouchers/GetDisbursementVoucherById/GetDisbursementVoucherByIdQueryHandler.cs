using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.GetDisbursementVoucherById;

public sealed class GetDisbursementVoucherByIdQueryHandler(
    BudgetDisbursementDbContext dbContext) : IQueryHandler<GetDisbursementVoucherByIdQuery, DisbursementVoucherDto>
{
    public async ValueTask<DisbursementVoucherDto> Handle(GetDisbursementVoucherByIdQuery query, CancellationToken cancellationToken)
    {
        var dv = await dbContext.DisbursementVouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Disbursement voucher '{query.Id}' not found.");

        return new DisbursementVoucherDto(
            dv.Id,
            dv.DvNumber,
            dv.DvDate,
            dv.PurchaseOrderId,
            dv.PurchaseOrderNumber,
            dv.BudgetUtilizationRequestId,
            dv.BurNumber,
            dv.FundCluster,
            dv.Payee,
            dv.TinNo,
            dv.PayeeAddress,
            dv.Particulars,
            dv.Amount,
            dv.ModeOfPayment,
            dv.Status,
            dv.Remarks,
            dv.PaidDate,
            dv.CreatedOnUtc.DateTime,
            dv.LastModifiedOnUtc?.DateTime,
            dv.Deductions
                .Select(d => new DvDeductionDto(d.Name, d.Type, d.Value, d.ComputeAmount(dv.Amount)))
                .ToList(),
            dv.TotalDeductions,
            dv.AmountDue);
    }
}

