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
    public void AssignTo_WhenPairIsIncomplete_Throws()
    {
        var vehicle = CreateVehicle();

        var act = () => vehicle.AssignTo(Guid.NewGuid(), null, null, null);

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
        VehicleAggregate.Create("tenant-1", "ABC-123", "Toyota", "Corolla", DateTime.UtcNow.Year, VehicleType.Sedan, 100);
}
