using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.UpdateTangibleInventory;

public sealed class UpdateTangibleInventoryCommandHandler(
    AssetManagementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<UpdateTangibleInventoryCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        UpdateTangibleInventoryCommand command,
        CancellationToken cancellationToken)
    {
        var inventory = await dbContext.TangibleInventories
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (inventory is null)
            throw new KeyNotFoundException($"Tangible Inventory with ID {command.Id} not found.");

        var reportNoConflict = await dbContext.TangibleInventories
            .AnyAsync(x => x.ReportNo == command.ReportNo && x.Id != command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (reportNoConflict)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(
                    nameof(command.ReportNo),
                    "A tangible inventory report with this number already exists.")
            ]);
        }

        inventory.Update(
            command.ReportNo,
            command.Date,
            command.ReceivedFrom,
            command.Address,
            command.ReceiptType,
            command.OtherReceiptType,
            command.FundCluster,
            command.ReceivedByEmployeeId,
            command.NotedByEmployeeId);

        inventory.LastModifiedBy = currentUser.GetUserId().ToString();
        inventory.LastModifiedOnUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

