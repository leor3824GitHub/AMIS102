using FluentValidation.Results;
using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Repairs;
using FSH.Modules.Vehicle.Features.v1.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.UpdateRepairRecord;

public sealed class UpdateRepairRecordCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdateRepairRecordCommand, RepairRecordDto>
{
    public async ValueTask<RepairRecordDto> Handle(UpdateRepairRecordCommand cmd, CancellationToken ct)
    {
        var record = await db.RepairRecords.FirstOrDefaultAsync(r => r.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Repair record not found.")]);

        if (record.Status == RepairStatus.Completed || record.Status == RepairStatus.Cancelled)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), $"Cannot update a repair record that is {record.Status}.")]);

        record.UpdateDetails(cmd.RepairDate, cmd.Description, cmd.Cost,
            cmd.VendorName, cmd.VendorContact, cmd.PartsUsed, cmd.Notes);
        record.LastModifiedBy = currentUser.GetUserId().ToString();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return record.ToDto();
    }
}
