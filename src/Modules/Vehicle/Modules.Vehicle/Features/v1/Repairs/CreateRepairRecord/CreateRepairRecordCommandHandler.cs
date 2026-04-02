using FluentValidation.Results;
using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Repairs;
using FSH.Modules.Vehicle.Features.v1.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.CreateRepairRecord;

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
        record.CreatedBy = currentUser.GetUserId().ToString();

        db.RepairRecords.Add(record);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return record.ToDto();
    }
}
