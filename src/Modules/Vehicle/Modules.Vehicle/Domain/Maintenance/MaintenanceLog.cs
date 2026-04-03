using FSH.Framework.Core.Domain;
using System.Security.Cryptography;

namespace FSH.Modules.Vehicle.Domain.Maintenance;

public class MaintenanceLog : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public Guid VehicleId { get; private set; }
    public Guid? ScheduleId { get; private set; }
    public string MaintenanceType { get; private set; } = default!;
    public DateOnly PerformedDate { get; private set; }
    public int? OdometerAtService { get; private set; }
    public string? Description { get; private set; }
    public decimal? Cost { get; private set; }
    public string? PerformedBy { get; private set; }
    public string? Notes { get; set; }
    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }

    public static MaintenanceLog Create(string tenantId, Guid vehicleId, Guid? scheduleId,
        string maintenanceType, DateOnly performedDate, int? odometerAtService,
        string? description, decimal? cost, string? performedBy, string? notes)
    {
        return new MaintenanceLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VehicleId = vehicleId,
            ScheduleId = scheduleId,
            MaintenanceType = maintenanceType,
            PerformedDate = performedDate,
            OdometerAtService = odometerAtService,
            Description = description,
            Cost = cost,
            PerformedBy = performedBy,
            Notes = notes,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };
    }

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);

    public void Update(string maintenanceType, DateOnly performedDate, int? odometerAtService,
        string? description, decimal? cost, string? performedBy, string? notes)
    {
        MaintenanceType = maintenanceType;
        PerformedDate = performedDate;
        OdometerAtService = odometerAtService;
        Description = description;
        Cost = cost;
        PerformedBy = performedBy;
        Notes = notes;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void SoftDelete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
        Version = NewVersion();
    }
}
