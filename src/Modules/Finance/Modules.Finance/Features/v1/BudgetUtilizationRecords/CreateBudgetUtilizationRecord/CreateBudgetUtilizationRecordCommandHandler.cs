using AMIS.Framework.Core.Context;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Data;
using AMIS.Modules.Finance.Domain.BudgetUtilizationRecords;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.CreateBudgetUtilizationRecord;

public sealed class CreateBudgetUtilizationRecordCommandHandler(
    ILogger<CreateBudgetUtilizationRecordCommandHandler> logger,
    FinanceDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateBudgetUtilizationRecordCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateBudgetUtilizationRecordCommand command, CancellationToken cancellationToken)
    {
        var burNumber = await GenerateBurNumberAsync(command.BurDate.Year, cancellationToken).ConfigureAwait(false);

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
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Created budget utilization record {BurNumber} for PO {PoNumber}", burNumber, command.PurchaseOrderNumber);

        return bur.Id;
    }

    private async Task<string> GenerateBurNumberAsync(int year, CancellationToken ct)
    {
        var prefix = $"BUR-{year}-";

        var lastNumber = await dbContext.BudgetUtilizationRecords
            .IgnoreQueryFilters()
            .Where(x => x.BurNumber.StartsWith(prefix))
            .Select(x => x.BurNumber)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (lastNumber != null && int.TryParse(lastNumber[prefix.Length..], out var last))
        {
            next = last + 1;
        }

        return $"{prefix}{next:00000}";
    }
}

