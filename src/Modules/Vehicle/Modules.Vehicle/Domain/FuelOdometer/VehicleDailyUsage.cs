using AMIS.Framework.Core.Domain;
using System.Security.Cryptography;

namespace AMIS.Modules.Vehicle.Domain.FuelOdometer;

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
    public decimal KmPerLiter => CalculateKmPerLiter(DistanceKm, FuelLiters);
    public decimal CostPerKm => CalculateCostPerKm(DistanceKm, FuelCost);
    public string? Destination { get; private set; }
    public string? Remarks { get; private set; }
    public byte[] Version { get; set; } = [];

    public DateTimeOffset CreatedOnUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

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

    internal void SetCreatedBy(string? userId)
    {
        CreatedBy = userId;
    }

    internal void SetLastModifiedBy(string? userId)
    {
        LastModifiedBy = userId;
    }

    public static decimal CalculateKmPerLiter(int distanceKm, decimal fuelLiters) =>
        fuelLiters > 0 ? Math.Round(distanceKm / fuelLiters, 4) : 0m;

    public static decimal CalculateCostPerKm(int distanceKm, decimal fuelCost) =>
        distanceKm > 0 ? Math.Round(fuelCost / distanceKm, 4) : 0m;

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

