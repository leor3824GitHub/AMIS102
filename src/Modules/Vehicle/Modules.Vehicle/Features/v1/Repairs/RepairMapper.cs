using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using FSH.Modules.Vehicle.Domain.Repairs;

namespace FSH.Modules.Vehicle.Features.v1.Repairs;

internal static class RepairMapper
{
    internal static RepairRecordDto ToDto(this RepairRecord r) =>
        new(r.Id, r.VehicleId, r.RepairDate, r.Description, r.Cost,
            r.VendorName, r.VendorContact, r.PartsUsed,
            r.Status.ToString(), r.CompletedDate, r.Notes,
            r.CreatedOnUtc, r.CreatedBy,
            r.LastModifiedOnUtc, r.LastModifiedBy);
}
