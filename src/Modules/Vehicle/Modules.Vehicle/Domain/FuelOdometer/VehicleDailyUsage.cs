using FSH.Framework.Core.Domain;
using System.Security.Cryptography;

namespace FSH.Modules.Vehicle.Domain.FuelOdometer;

public sealed class VehicleDailyUsage : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public Guid VehicleId { get; private set; }
    public DateOnly Date { get; private set; }
    public int OdometerStart { get; private set; }
    public int OdometerEnd { get; private set; }
    public int DistanceKm { get; private set; }
    public decimal FuelLiters { get; private set; }
    public decimal FuelCost { get; private set; }
    public string? Destination { get; private set; }
    public string? Remarks { get; private set; }
    public byte[] Version { get; set; } = [];

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }

    public static VehicleDailyUsage Create(
        string tenantId,
        Guid vehicleId,
        DateOnly date,
        int odometerStart,
        int odometerEnd,
        decimal fuelLiters,
        decimal fuelCost,
        string? destination,
        string? remarks)
    {
        EnsureValid(odometerStart, odometerEnd, fuelLiters, fuelCost);

        return new VehicleDailyUsage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VehicleId = vehicleId,
            Date = date,
            OdometerStart = odometerStart,
            OdometerEnd = odometerEnd,
            DistanceKm = odometerEnd - odometerStart,
            FuelLiters = fuelLiters,
            FuelCost = fuelCost,
            Destination = destination,
            Remarks = remarks,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };
    }

    public void Update(
        DateOnly date,
        int odometerStart,
        int odometerEnd,
        decimal fuelLiters,
        decimal fuelCost,
        string? destination,
        string? remarks)
    {
        EnsureValid(odometerStart, odometerEnd, fuelLiters, fuelCost);

        Date = date;
        OdometerStart = odometerStart;
        OdometerEnd = odometerEnd;
        DistanceKm = odometerEnd - odometerStart;
        FuelLiters = fuelLiters;
        FuelCost = fuelCost;
        Destination = destination;
        Remarks = remarks;
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

    private static void EnsureValid(int odometerStart, int odometerEnd, decimal fuelLiters, decimal fuelCost)
    {
        if (odometerStart < 0)
            throw new InvalidOperationException("Odometer start must be zero or greater.");

        if (odometerEnd < odometerStart)
            throw new InvalidOperationException("Odometer end cannot be less than odometer start.");

        if (fuelLiters < 0)
            throw new InvalidOperationException("Fuel liters must be zero or greater.");

        if (fuelCost < 0)
            throw new InvalidOperationException("Fuel cost must be zero or greater.");
    }

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);
}
