namespace FSH.Modules.Vehicle;

public static class VehicleModuleConstants
{
    public const string SchemaName = "vehicle";
    public const string MigrationsTable = "__EFMigrationsHistory";

    public static class Permissions
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

        public static class Repairs
        {
            public const string View   = "Permissions.Vehicle.Repairs.View";
            public const string Create = "Permissions.Vehicle.Repairs.Create";
            public const string Update = "Permissions.Vehicle.Repairs.Update";
            public const string Delete = "Permissions.Vehicle.Repairs.Delete";
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
    }
}
