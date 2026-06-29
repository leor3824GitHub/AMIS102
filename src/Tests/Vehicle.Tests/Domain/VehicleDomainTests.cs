using Shouldly;
using VehicleAggregate = AMIS.Modules.Vehicle.Domain.Vehicles.Vehicle;
using VehicleType = AMIS.Modules.Vehicle.Domain.Vehicles.VehicleType;
using Xunit;

namespace Vehicle.Tests.Domain;

public sealed class VehicleDomainTests
{
    [Fact]
    public void Retire_WhenVehicleIsNotActive_Throws()
    {
        var vehicle = CreateVehicle();
        vehicle.Decommission();

        var act = vehicle.Retire;

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void UpdateOdometer_WhenReadingDecreases_Throws()
    {
        var vehicle = CreateVehicle();

        var act = () => vehicle.UpdateOdometer(50);

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Reactivate_WhenVehicleIsRetired_SetsStatusToActive()
    {
        var vehicle = CreateVehicle();
        vehicle.Retire();

        vehicle.Reactivate();

        vehicle.Status.ShouldBe(AMIS.Modules.Vehicle.Domain.Vehicles.VehicleStatus.Active);
    }

    private static VehicleAggregate CreateVehicle() =>
        VehicleAggregate.Enroll("tenant-1", Guid.NewGuid(), "2026-06-LT-0001", 950000m,
            new DateOnly(2020, 1, 1), "ABC-123", "Toyota", "Corolla", DateTime.UtcNow.Year, VehicleType.Sedan, 100);
}
