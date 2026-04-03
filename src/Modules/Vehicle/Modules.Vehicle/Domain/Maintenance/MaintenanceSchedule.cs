using FSH.Framework.Core.Domain;
using System.Security.Cryptography;

namespace FSH.Modules.Vehicle.Domain.Maintenance;

public class MaintenanceSchedule : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public Guid VehicleId { get; private set; }
    public string MaintenanceType { get; private set; } = default!;
    public string? Description { get; private set; }
    public int? IntervalDays { get; private set; }
    public int? IntervalMileage { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public int? DueMileage { get; private set; }
    public DateOnly? LastDoneDate { get; private set; }
    public int? LastDoneMileage { get; private set; }
    public bool IsActive { get; private set; } = true;
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

    public static MaintenanceSchedule Create(string tenantId, Guid vehicleId, string maintenanceType,
        string? description, int? intervalDays, int? intervalMileage,
        DateOnly? initialDueDate, int? initialDueMileage)
    {
        return new MaintenanceSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VehicleId = vehicleId,
            MaintenanceType = maintenanceType,
            Description = description,
            IntervalDays = intervalDays,
            IntervalMileage = intervalMileage,
            DueDate = initialDueDate,
            DueMileage = initialDueMileage,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };
    }

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);

    public void Update(string maintenanceType, string? description,
        int? intervalDays, int? intervalMileage, DateOnly? dueDate, int? dueMileage)
    {
        MaintenanceType = maintenanceType;
        Description = description;
        IntervalDays = intervalDays;
        IntervalMileage = intervalMileage;
        DueDate = dueDate;
        DueMileage = dueMileage;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    /// <summary>Records a completed maintenance and advances the next due date/mileage.</summary>
    public void RecordCompletion(DateOnly doneDate, int? doneMileage)
    {
        LastDoneDate = doneDate;
        LastDoneMileage = doneMileage;

        if (IntervalDays.HasValue)
            DueDate = doneDate.AddDays(IntervalDays.Value);

        if (IntervalMileage.HasValue && doneMileage.HasValue)
            DueMileage = doneMileage.Value + IntervalMileage.Value;

        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void Activate()
    {
        IsActive = true;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void Deactivate()
    {
        IsActive = false;
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
