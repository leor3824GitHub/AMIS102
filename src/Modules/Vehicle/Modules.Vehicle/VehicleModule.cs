using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Persistence;
using FSH.Framework.Web.Modules;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Features.v1.Vehicles.CreateVehicle;
using FSH.Modules.Vehicle.Features.v1.Vehicles.UpdateVehicle;
using FSH.Modules.Vehicle.Features.v1.Vehicles.AssignVehicle;
using FSH.Modules.Vehicle.Features.v1.Vehicles.UpdateOdometer;
using FSH.Modules.Vehicle.Features.v1.Vehicles.RetireVehicle;
using FSH.Modules.Vehicle.Features.v1.Vehicles.DecommissionVehicle;
using FSH.Modules.Vehicle.Features.v1.Vehicles.ReactivateVehicle;
using FSH.Modules.Vehicle.Features.v1.Vehicles.DeleteVehicle;
using FSH.Modules.Vehicle.Features.v1.Vehicles.GetVehicle;
using FSH.Modules.Vehicle.Features.v1.Vehicles.SearchVehicles;
using FSH.Modules.Vehicle.Features.v1.Vehicles.GetMotorVehicleInventory;
using FSH.Modules.Vehicle.Features.v1.Repairs.CreateRepairRecord;
using FSH.Modules.Vehicle.Features.v1.Repairs.UpdateRepairRecord;
using FSH.Modules.Vehicle.Features.v1.Repairs.StartRepair;
using FSH.Modules.Vehicle.Features.v1.Repairs.CompleteRepair;
using FSH.Modules.Vehicle.Features.v1.Repairs.CancelRepair;
using FSH.Modules.Vehicle.Features.v1.Repairs.DeleteRepairRecord;
using FSH.Modules.Vehicle.Features.v1.Repairs.GetRepairRecord;
using FSH.Modules.Vehicle.Features.v1.Repairs.SearchRepairRecords;
using FSH.Modules.Vehicle.Features.v1.Maintenance.CreateMaintenanceSchedule;
using FSH.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceSchedule;
using FSH.Modules.Vehicle.Features.v1.Maintenance.DeactivateMaintenanceSchedule;
using FSH.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceSchedule;
using FSH.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceSchedule;
using FSH.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceSchedules;
using FSH.Modules.Vehicle.Features.v1.Maintenance.GetDueMaintenanceSchedules;
using FSH.Modules.Vehicle.Features.v1.Maintenance.LogMaintenanceCompletion;
using FSH.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceLog;
using FSH.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceLog;
using FSH.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceLog;
using FSH.Modules.Vehicle.Features.v1.Lookups;
using FSH.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceLogs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.Vehicle;

public class VehicleModule : IModule
{
    private static readonly IReadOnlyList<FshPermission> RegisteredPermissions =
    [
        new("View Vehicle Lookup", "View", "Vehicle.Lookup", IsBasic: true),

        new("View Vehicles", "View", "Vehicle.Vehicles", IsBasic: true),
        new("Create Vehicles", "Create", "Vehicle.Vehicles"),
        new("Update Vehicles", "Update", "Vehicle.Vehicles"),
        new("Delete Vehicles", "Delete", "Vehicle.Vehicles"),

        new("View Vehicle Repairs", "View", "Vehicle.Repairs", IsBasic: true),
        new("Create Vehicle Repairs", "Create", "Vehicle.Repairs"),
        new("Update Vehicle Repairs", "Update", "Vehicle.Repairs"),
        new("Delete Vehicle Repairs", "Delete", "Vehicle.Repairs"),

        new("View Vehicle Maintenance", "View", "Vehicle.Maintenance", IsBasic: true),
        new("Create Vehicle Maintenance", "Create", "Vehicle.Maintenance"),
        new("Update Vehicle Maintenance", "Update", "Vehicle.Maintenance"),
        new("Delete Vehicle Maintenance", "Delete", "Vehicle.Maintenance"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(RegisteredPermissions);

        builder.Services.AddHeroDbContext<VehicleDbContext>();
        builder.Services.AddScoped<IDbInitializer, VehicleDbInitializer>();
        builder.Services.AddHostedService<Provisioning.VehicleDbInitializerHostedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/vehicle")
            .WithTags("Vehicle")
            .WithApiVersionSet(apiVersionSet);

        var lookupGroup = moduleGroup.MapGroup("/lookup");
        var vehiclesGroup = moduleGroup.MapGroup("/vehicles");
        var repairsGroup = moduleGroup.MapGroup("/repairs");
        var maintenanceGroup = moduleGroup.MapGroup("/maintenance");

        // Lookup endpoints
        VehicleLookupEndpoint.Map(lookupGroup);

        // Vehicle endpoints
        CreateVehicleEndpoint.Map(vehiclesGroup);
        UpdateVehicleEndpoint.Map(vehiclesGroup);
        AssignVehicleEndpoint.Map(vehiclesGroup);
        UpdateOdometerEndpoint.Map(vehiclesGroup);
        RetireVehicleEndpoint.Map(vehiclesGroup);
        DecommissionVehicleEndpoint.Map(vehiclesGroup);
        ReactivateVehicleEndpoint.Map(vehiclesGroup);
        DeleteVehicleEndpoint.Map(vehiclesGroup);
        GetVehicleEndpoint.Map(vehiclesGroup);
        SearchVehiclesEndpoint.Map(vehiclesGroup);
        GetMotorVehicleInventoryEndpoint.Map(vehiclesGroup);

        // Repair endpoints
        CreateRepairRecordEndpoint.Map(repairsGroup);
        UpdateRepairRecordEndpoint.Map(repairsGroup);
        StartRepairEndpoint.Map(repairsGroup);
        CompleteRepairEndpoint.Map(repairsGroup);
        CancelRepairEndpoint.Map(repairsGroup);
        DeleteRepairRecordEndpoint.Map(repairsGroup);
        GetRepairRecordEndpoint.Map(repairsGroup);
        SearchRepairRecordsEndpoint.Map(repairsGroup);

        // Maintenance Schedule endpoints
        CreateMaintenanceScheduleEndpoint.Map(maintenanceGroup);
        UpdateMaintenanceScheduleEndpoint.Map(maintenanceGroup);
        DeactivateMaintenanceScheduleEndpoint.Map(maintenanceGroup);
        DeleteMaintenanceScheduleEndpoint.Map(maintenanceGroup);
        GetMaintenanceScheduleEndpoint.Map(maintenanceGroup);
        SearchMaintenanceSchedulesEndpoint.Map(maintenanceGroup);
        GetDueMaintenanceSchedulesEndpoint.Map(maintenanceGroup);

        // Maintenance Log endpoints
        LogMaintenanceCompletionEndpoint.Map(maintenanceGroup);
        UpdateMaintenanceLogEndpoint.Map(maintenanceGroup);
        DeleteMaintenanceLogEndpoint.Map(maintenanceGroup);
        GetMaintenanceLogEndpoint.Map(maintenanceGroup);
        SearchMaintenanceLogsEndpoint.Map(maintenanceGroup);
    }
}
