using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Data;
using AMIS.Modules.Finance.Domain.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Features.v1.Shared;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.CreateBudgetUtilizationRecord;

public sealed class CreateBudgetUtilizationRecordCommandHandler(
    ILogger<CreateBudgetUtilizationRecordCommandHandler> logger,
    FinanceDbContext dbContext,
    IMediator mediator,
    ICurrentUser currentUser) : ICommandHandler<CreateBudgetUtilizationRecordCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateBudgetUtilizationRecordCommand command, CancellationToken cancellationToken)
    {
        // The BUR obligates budget against a real purchase order — reject a bogus PO reference.
        _ = await mediator.Send(new GetPurchaseOrderQuery(command.PurchaseOrderId), cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Purchase order not found.", [], HttpStatusCode.NotFound);

        // When the BUR is linked to a disbursement voucher, that voucher must exist.
        if (command.DisbursementVoucherId is { } dvId && dvId != Guid.Empty)
        {
            var dvExists = await dbContext.DisbursementVouchers
                .AnyAsync(d => d.Id == dvId, cancellationToken).ConfigureAwait(false);
            if (!dvExists)
                throw new CustomException("Linked disbursement voucher not found.", [], HttpStatusCode.NotFound);
        }

        // Atomic number allocation: increment a per-year counter row guarded by xmin, retrying on conflict.
        for (var attempt = 0; attempt < FinanceSequenceAllocation.MaxAttempts; attempt++)
        {
            var year = command.BurDate.Year;

            var sequence = await dbContext.BurNumberSequences
                .FirstOrDefaultAsync(x => x.Year == year, cancellationToken)
                .ConfigureAwait(false);

            if (sequence is null)
            {
                sequence = BurNumberSequence.Create(year);
                dbContext.BurNumberSequences.Add(sequence);
            }

            var serial = sequence.NextSerial();
            var burNumber = $"BUR-{year}-{serial:00000}";

            var bur = BudgetUtilizationRecord.Create(
                burNumber,
                command.PurchaseOrderId,
                command.PurchaseOrderNumber,
                command.DisbursementVoucherId,
                command.DisbursementVoucherNumber,
                command.BurDate,
                command.AllotmentClass,
                command.UacsObjectCode,
                command.ResponsibilityCenter,
                command.Particulars,
                command.Amount,
                command.Remarks);

            bur.CreatedBy = currentUser.GetUserId().ToString();
            dbContext.BudgetUtilizationRecords.Add(bur);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Created budget utilization record {BurNumber} for PO {PoNumber}", burNumber, command.PurchaseOrderNumber);
                return bur.Id;
            }
            catch (DbUpdateException ex) when (FinanceSequenceAllocation.IsRetryableAllocationConflict(ex))
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        throw new CustomException("Failed to allocate a unique BUR number. Please try again.", [], HttpStatusCode.Conflict);
    }
}

