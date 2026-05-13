using FSH.Framework.Core.Domain;
using System.Security.Cryptography;

namespace FSH.Modules.Vehicle.Domain.Repairs;

public enum RepairStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public class RepairRecord : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public Guid VehicleId { get; private set; }
    public DateTimeOffset RepairDate { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Cost { get; private set; }
    public string? VendorName { get; private set; }
    public string? VendorContact { get; private set; }
    public string? PartsUsed { get; private set; }
    public RepairStatus Status { get; private set; } = RepairStatus.Pending;
    public DateTimeOffset? CompletedDate { get; private set; }
    public string? Notes { get; set; }
    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    // ISoftDeletable
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public static RepairRecord Create(string tenantId, Guid vehicleId, DateTimeOffset repairDate,
        string description, decimal cost, string? vendorName = null, string? vendorContact = null,
        string? partsUsed = null, string? notes = null)
    {
        return new RepairRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VehicleId = vehicleId,
            RepairDate = repairDate,
            Description = description,
            Cost = cost,
            VendorName = vendorName,
            VendorContact = vendorContact,
            PartsUsed = partsUsed,
            Notes = notes,
            Status = RepairStatus.Pending,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };
    }

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);

    public void UpdateDetails(DateTimeOffset repairDate, string description, decimal cost,
        string? vendorName, string? vendorContact, string? partsUsed, string? notes)
    {
        RepairDate = repairDate;
        Description = description;
        Cost = cost;
        VendorName = vendorName;
        VendorContact = vendorContact;
        PartsUsed = partsUsed;
        Notes = notes;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void StartRepair()
    {
        Status = RepairStatus.InProgress;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void Complete(DateTimeOffset completedDate)
    {
        Status = RepairStatus.Completed;
        CompletedDate = completedDate;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void Cancel()
    {
        Status = RepairStatus.Cancelled;
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

    internal void SetCreatedBy(string? userId)
    {
        CreatedBy = userId;
    }

    internal void SetLastModifiedBy(string? userId)
    {
        LastModifiedBy = userId;
    }
}
