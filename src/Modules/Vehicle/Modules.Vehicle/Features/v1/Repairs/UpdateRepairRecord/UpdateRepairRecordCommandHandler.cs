using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Repairs;
using AMIS.Modules.Vehicle.Features.v1.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.UpdateRepairRecord;

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
        record.SetLastModifiedBy(currentUser.GetUserId().ToString());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return record.ToDto();
    }
}

