using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Persistence.Sequencing;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Data;
using AMIS.Modules.BudgetDisbursement.Domain.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Features.v1.Shared;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.CreateBudgetUtilizationRequest;

public sealed class CreateBudgetUtilizationRequestCommandHandler(
    ILogger<CreateBudgetUtilizationRequestCommandHandler> logger,
    BudgetDisbursementDbContext dbContext,
    IMediator mediator,
    ICurrentUser currentUser) : ICommandHandler<CreateBudgetUtilizationRequestCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateBudgetUtilizationRequestCommand command, CancellationToken cancellationToken)
    {
        // The BUR obligates budget against a real purchase order — reject a bogus PO reference. The
        // disbursement voucher is not referenced here: it is raised later against this BUR (BUR first, then DV).
        _ = await mediator.Send(new GetPurchaseOrderQuery(command.PurchaseOrderId), cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Purchase order not found.", [], HttpStatusCode.NotFound);

        // Atomic number allocation: increment a per-year counter row (global — TenantId "") guarded by xmin,
        // retrying on conflict.
        var year = command.BurDate.Year;
        for (var attempt = 0; attempt < SequenceAllocator.MaxAttempts; attempt++)
        {
            var serial = await SequenceAllocator.ReserveNextSerialAsync(
                dbContext, tenantId: string.Empty, sequenceKey: "BUR", year, month: 0, cancellationToken,
                seedFactory: SeedFromIssuedAsync).ConfigureAwait(false);

            var burNumber = $"BUR-{year}-{serial:00000}";

            var bur = BudgetUtilizationRequest.Create(
                currentUser.GetTenant() ?? string.Empty,
                burNumber,
                command.PurchaseOrderId,
                command.PurchaseOrderNumber,
                command.BurDate,
                command.FundCluster,
                command.AllotmentClass,
                command.UacsObjectCode,
                command.ResponsibilityCenter,
                command.Particulars,
                command.Amount,
                command.Remarks);

            bur.CreatedBy = currentUser.GetUserId().ToString();
            dbContext.BudgetUtilizationRequests.Add(bur);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Created budget utilization record {BurNumber} for PO {PoNumber}", burNumber, command.PurchaseOrderNumber);
                return bur.Id;
            }
            catch (DbUpdateException ex) when (SequenceAllocator.IsRetryableAllocationConflict(ex))
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        throw new CustomException("Failed to allocate a unique BUR number. Please try again.", [], HttpStatusCode.Conflict);

        // Seeds a first-created counter row from the highest BUR number already issued this year — including
        // soft-deleted rows, which the global unique index still enforces — so allocation never re-issues one.
        async Task<int> SeedFromIssuedAsync()
        {
            var prefix = $"BUR-{year}-";
            var issuedNumbers = await dbContext.BudgetUtilizationRequests
                .IgnoreQueryFilters()
                .Where(b => b.BurNumber.StartsWith(prefix))
                .Select(b => b.BurNumber)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return issuedNumbers.Count == 0 ? 0 : issuedNumbers.Max(BudgetDocumentNumber.ParseSerial);
        }
    }
}

