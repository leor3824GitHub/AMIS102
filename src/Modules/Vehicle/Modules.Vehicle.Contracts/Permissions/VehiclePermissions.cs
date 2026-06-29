namespace AMIS.Modules.Vehicle.Contracts.Permissions;

public static class VehiclePermissions
{
    public static class Lookup
    {
        public const string View = "Permissions.Vehicle.Lookup.View";
    }

    public static class Vehicles
    {
        public const string View   = "Permissions.Vehicle.Vehicles.View";
        public const string Create = "Permissions.Vehicle.Vehicles.Create";
        public const string Update = "Permissions.Vehicle.Vehicles.Update";
        public const string Delete = "Permissions.Vehicle.Vehicles.Delete";
    }

    public static class Maintenance
    {
        public const string View   = "Permissions.Vehicle.Maintenance.View";
        public const string Create = "Permissions.Vehicle.Maintenance.Create";
        public const string Update = "Permissions.Vehicle.Maintenance.Update";
        public const string Delete = "Permissions.Vehicle.Maintenance.Delete";
    }

    public static class FuelOdometer
    {
        public const string View = "Permissions.Vehicle.FuelOdometer.View";
        public const string Create = "Permissions.Vehicle.FuelOdometer.Create";
        public const string Update = "Permissions.Vehicle.FuelOdometer.Update";
    }

    /// <summary>Accountable-officer self-service: act only on vehicles PAR'd to the current user.</summary>
    public static class MyVehicle
    {
        public const string View = "Permissions.Vehicle.MyVehicle.View";
        public const string RecordFuelOdometer = "Permissions.Vehicle.MyVehicle.RecordFuelOdometer";
    }
}
