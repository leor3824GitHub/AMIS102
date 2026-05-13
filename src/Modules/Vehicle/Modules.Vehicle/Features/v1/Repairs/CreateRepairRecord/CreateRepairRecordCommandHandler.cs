using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Repairs;
using AMIS.Modules.Vehicle.Features.v1.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.CreateRepairRecord;

public sealed class CreateRepairRecordCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<CreateRepairRecordCommand, RepairRecordDto>
{
    public async ValueTask<RepairRecordDto> Handle(CreateRepairRecordCommand cmd, CancellationToken ct)
    {
        var tenantId = currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required");

        var vehicleExists = await db.Vehicles
            .AnyAsync(v => v.Id == cmd.VehicleId, ct)
            .ConfigureAwait(false);

        if (!vehicleExists)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.VehicleId), "Vehicle not found.")]);

        var record = RepairRecord.Create(tenantId, cmd.VehicleId, cmd.RepairDate,
            cmd.Description, cmd.Cost, cmd.VendorName, cmd.VendorContact, cmd.PartsUsed, cmd.Notes);
        record.SetCreatedBy(currentUser.GetUserId().ToString());

        db.RepairRecords.Add(record);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return record.ToDto();
    }
}

