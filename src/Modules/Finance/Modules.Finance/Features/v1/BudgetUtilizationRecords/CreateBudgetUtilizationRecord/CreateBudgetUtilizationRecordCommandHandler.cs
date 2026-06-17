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
        // The BUR obligates budget against a real purchase order — reject a bogus PO reference. The
        // disbursement voucher is not referenced here: it is raised later against this BUR (BUR first, then DV).
        _ = await mediator.Send(new GetPurchaseOrderQuery(command.PurchaseOrderId), cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Purchase order not found.", [], HttpStatusCode.NotFound);

        // Atomic number allocation: increment a per-year counter row guarded by xmin, retrying on conflict.
        for (var attempt = 0; attempt < FinanceSequenceAllocation.MaxAttempts; attempt++)
        {
            var year = command.BurDate.Year;

            var sequence = await dbContext.BurNumberSequences
                .FirstOrDefaultAsync(x => x.Year == year, cancellationToken)
                .ConfigureAwait(false);

            if (sequence is null)
            {
                // No counter row for this year yet (fresh year, or the table was reset while records
                // remained). Seed it from the highest BUR number already issued — including soft-deleted
                // rows, which the global unique index still enforces — so we never re-issue an existing one.
                var prefix = $"BUR-{year}-";
                var issuedNumbers = await dbContext.BudgetUtilizationRecords
                    .IgnoreQueryFilters()
                    .Where(b => b.BurNumber.StartsWith(prefix))
                    .Select(b => b.BurNumber)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                var lastSerial = issuedNumbers.Count == 0
                    ? 0
                    : issuedNumbers.Max(FinanceSequenceAllocation.ParseSerial);

                sequence = BurNumberSequence.Create(year, lastSerial);
                dbContext.BurNumberSequences.Add(sequence);
            }

            var serial = sequence.NextSerial();
            var burNumber = $"BUR-{year}-{serial:00000}";

            var bur = BudgetUtilizationRecord.Create(
                burNumber,
                command.PurchaseOrderId,
                command.PurchaseOrderNumber,
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

