using FSH.Framework.Core.Domain;
using FSH.Modules.Vehicle.Domain.Events;
using System.Security.Cryptography;

namespace FSH.Modules.Vehicle.Domain.Vehicles;

public enum VehicleType
{
    Other = 0,
    Sedan = 1,
    SUV = 2,
    Van = 3,
    Truck = 4,
    Motorcycle = 5,
    Bus = 6,
    PickUp = 7,
    MPV = 8,
    Hatchback = 9,
    Crossover = 10,
    Wagon = 11,
    Minibus = 12
}

public enum VehicleStatus
{
    Active = 1,
    UnderRepair = 2,
    Retired = 3,
    Decommissioned = 4
}

public class Vehicle : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string PlateNumber { get; private set; } = default!;
    public string Make { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public int Year { get; private set; }
    public VehicleType Type { get; private set; }
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;
    public int Odometer { get; private set; }

    // Technical specifications (for motor vehicle inventory report)
    public string? MotorNumber { get; private set; }
    public string? ChassisNumber { get; private set; }
    public int? NumberOfCylinders { get; private set; }
    public int? EngineDisplacementCC { get; private set; }
    public string? FuelType { get; private set; }       // Diesel, Gasoline, Electric, Hybrid
    public string? VehicleUse { get; private set; }     // e.g. GOV'T-UV, GOV'T-MPV
    public decimal? AcquisitionCost { get; private set; }

    public Guid? AssignedDepartmentId { get; private set; }
    public string? AssignedDepartment { get; private set; }
    public Guid? AssignedDriverId { get; private set; }
    public string? AssignedDriver { get; private set; }
    public string? AccountableOfficerTitle { get; private set; }  // Driver designation, e.g. Truck Driver

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

    public static Vehicle Create(string tenantId, string plateNumber, string make, string model,
        int year, VehicleType type, int odometer = 0, string? notes = null,
        string? motorNumber = null, string? chassisNumber = null,
        int? numberOfCylinders = null, int? engineDisplacementCC = null,
        string? fuelType = null, string? vehicleUse = null, decimal? acquisitionCost = null)
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlateNumber = plateNumber.ToUpperInvariant(),
            Make = make,
            Model = model,
            Year = year,
            Type = type,
            Status = VehicleStatus.Active,
            Odometer = odometer,
            Notes = notes,
            MotorNumber = motorNumber,
            ChassisNumber = chassisNumber,
            NumberOfCylinders = numberOfCylinders,
            EngineDisplacementCC = engineDisplacementCC,
            FuelType = fuelType,
            VehicleUse = vehicleUse,
            AcquisitionCost = acquisitionCost,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };

        vehicle.AddDomainEvent(VehicleCreatedEvent.Create(vehicle.Id, tenantId, vehicle.PlateNumber, vehicle.Make, vehicle.Model, vehicle.Year));
        return vehicle;
    }

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);

    public void Update(string plateNumber, string make, string model, int year, VehicleType type, string? notes,
        string? motorNumber = null, string? chassisNumber = null,
        int? numberOfCylinders = null, int? engineDisplacementCC = null,
        string? fuelType = null, string? vehicleUse = null, decimal? acquisitionCost = null)
    {
        PlateNumber = plateNumber.ToUpperInvariant();
        Make = make;
        Model = model;
        Year = year;
        Type = type;
        Notes = notes;
        MotorNumber = motorNumber;
        ChassisNumber = chassisNumber;
        NumberOfCylinders = numberOfCylinders;
        EngineDisplacementCC = engineDisplacementCC;
        FuelType = fuelType;
        VehicleUse = vehicleUse;
        AcquisitionCost = acquisitionCost;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void AssignTo(Guid? departmentId, string? departmentName, Guid? driverId, string? driverName,
        string? accountableOfficerTitle = null)
    {
        ValidateAssignmentPair(departmentId, departmentName, "department");
        ValidateAssignmentPair(driverId, driverName, "driver");

        AssignedDepartmentId = departmentId;
        AssignedDepartment = departmentName;
        AssignedDriverId = driverId;
        AssignedDriver = driverName;
        AccountableOfficerTitle = accountableOfficerTitle;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void UpdateOdometer(int reading)
    {
        if (reading < Odometer)
            throw new InvalidOperationException("Odometer reading cannot be less than the current reading.");

        Odometer = reading;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void MarkUnderRepair()
    {
        if (Status == VehicleStatus.UnderRepair)
            return;

        if (Status != VehicleStatus.Active)
            throw new InvalidOperationException("Only active vehicles can be moved to under repair.");

        Status = VehicleStatus.UnderRepair;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void Reactivate()
    {
        if (Status == VehicleStatus.Active)
            throw new InvalidOperationException("Vehicle is already active.");

        if (Status != VehicleStatus.Retired && Status != VehicleStatus.Decommissioned && Status != VehicleStatus.UnderRepair)
            throw new InvalidOperationException("Vehicle cannot be reactivated from its current status.");

        Status = VehicleStatus.Active;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
        AddDomainEvent(VehicleReactivatedEvent.Create(Id, TenantId, PlateNumber));
    }

    public void Retire()
    {
        if (Status == VehicleStatus.Retired)
            throw new InvalidOperationException("Vehicle is already retired.");

        if (Status != VehicleStatus.Active)
            throw new InvalidOperationException("Only active vehicles can be retired.");

        Status = VehicleStatus.Retired;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
        AddDomainEvent(VehicleRetiredEvent.Create(Id, TenantId, PlateNumber));
    }

    public void Decommission()
    {
        if (Status == VehicleStatus.Decommissioned)
            throw new InvalidOperationException("Vehicle is already decommissioned.");

        if (Status != VehicleStatus.Active && Status != VehicleStatus.Retired)
            throw new InvalidOperationException("Only active or retired vehicles can be decommissioned.");

        Status = VehicleStatus.Decommissioned;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
        AddDomainEvent(VehicleDecommissionedEvent.Create(Id, TenantId, PlateNumber));
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

    internal void SetLastModifiedOnUtc(DateTimeOffset utcNow)
    {
        LastModifiedOnUtc = utcNow;
    }

    private static void ValidateAssignmentPair(Guid? id, string? name, string label)
    {
        var hasId = id.HasValue;
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (hasId != hasName)
            throw new InvalidOperationException($"{label} ID and name must both be provided or both omitted.");
    }
}
