using AMIS.Modules.Vehicle.Contracts.Permissions;
using MudBlazor;

namespace AMIS.Blazor.Components.Layout.Nav;

/// <summary>
/// The Vehicle module's side-nav entries, declared as data.
/// </summary>
/// <remarks>
/// Prototype for the data-driven nav. Compare with the hand-written groups in
/// <c>NavMenu.razor</c>: there, a group's visibility, its expansion field, and its children's
/// permission gates are three separate statements that must be kept in agreement by hand. Here
/// all three are derived from this one declaration.
/// </remarks>
public static class VehicleNav
{
    /// <summary>Top-level "Vehicle" group.</summary>
    public static NavGroup Section { get; } =
        new("vehicle", "Vehicle", Icons.Material.Outlined.DirectionsCar)
        {
            Entries =
            [
                new NavItem("My Vehicle", "/vehicle/my-vehicles", Icons.Material.Outlined.NoCrash)
                {
                    Permission = VehiclePermissions.MyVehicle.View,
                },
                new NavItem("Vehicles", "/vehicle/vehicles", Icons.Material.Outlined.DirectionsCarFilled),

                // The shared "Maintenance" prefix lives on the parent so the children stay
                // single-line: Schedules / Due / Logs.
                new NavGroup("vehicle.maintenance", "Maintenance", Icons.Material.Outlined.Handyman)
                {
                    Entries =
                    [
                        new NavItem("Schedules", "/vehicle/maintenance/schedules", Icons.Material.Outlined.EventRepeat),
                        new NavItem("Due", "/vehicle/maintenance/due", Icons.Material.Outlined.WarningAmber),
                        new NavItem("Logs", "/vehicle/maintenance/logs", Icons.Material.Outlined.HistoryEdu),
                    ],
                },

                new NavItem("Fuel & Odometer", "/vehicle/fuel-odometer", Icons.Material.Outlined.LocalGasStation),
                new NavItem("Inventory Report", "/vehicle/reports/inventory", Icons.Material.Outlined.ListAlt),
            ],
        };
}
