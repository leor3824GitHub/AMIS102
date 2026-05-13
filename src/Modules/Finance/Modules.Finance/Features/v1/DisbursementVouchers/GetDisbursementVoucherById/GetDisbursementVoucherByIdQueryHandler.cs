using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.GetDisbursementVoucherById;

public sealed class GetDisbursementVoucherByIdQueryHandler(
    FinanceDbContext dbContext) : IQueryHandler<GetDisbursementVoucherByIdQuery, DisbursementVoucherDto>
{
    public async ValueTask<DisbursementVoucherDto> Handle(GetDisbursementVoucherByIdQuery query, CancellationToken cancellationToken)
    {
        var dv = await dbContext.DisbursementVouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Disbursement voucher '{query.Id}' not found.");

        return new DisbursementVoucherDto(
            dv.Id,
            dv.DvNumber,
            dv.DvDate,
            dv.PurchaseOrderId,
            dv.PurchaseOrderNumber,
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
            dv.LastModifiedOnUtc?.DateTime);
    }
}

