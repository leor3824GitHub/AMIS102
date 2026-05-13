using Asp.Versioning;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Shared.Persistence;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.CreateVehicle;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.UpdateVehicle;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.AssignVehicle;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.UpdateOdometer;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.RetireVehicle;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.DecommissionVehicle;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.ReactivateVehicle;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.DeleteVehicle;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.GetVehicle;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.SearchVehicles;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.GetMotorVehicleInventory;
using AMIS.Modules.Vehicle.Features.v1.Vehicles.GenerateVehicleInventoryPdf;
using AMIS.Modules.Vehicle.Features.v1.FuelOdometer.CreateVehicleDailyUsage;
using AMIS.Modules.Vehicle.Features.v1.FuelOdometer.UpdateVehicleDailyUsage;
using AMIS.Modules.Vehicle.Features.v1.FuelOdometer.SearchVehicleDailyUsage;
using AMIS.Modules.Vehicle.Features.v1.FuelOdometer.GetVehicleDailyUsageSummary;
using AMIS.Modules.Vehicle.Features.v1.Repairs.CreateRepairRecord;
using AMIS.Modules.Vehicle.Features.v1.Repairs.UpdateRepairRecord;
using AMIS.Modules.Vehicle.Features.v1.Repairs.StartRepair;
using AMIS.Modules.Vehicle.Features.v1.Repairs.CompleteRepair;
using AMIS.Modules.Vehicle.Features.v1.Repairs.CancelRepair;
using AMIS.Modules.Vehicle.Features.v1.Repairs.DeleteRepairRecord;
using AMIS.Modules.Vehicle.Features.v1.Repairs.GetRepairRecord;
using AMIS.Modules.Vehicle.Features.v1.Repairs.SearchRepairRecords;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.CreateMaintenanceSchedule;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceSchedule;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.DeactivateMaintenanceSchedule;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceSchedule;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceSchedule;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceSchedules;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.GetDueMaintenanceSchedules;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.LogMaintenanceCompletion;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceLog;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceLog;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceLog;
using AMIS.Modules.Vehicle.Features.v1.Lookups;
using AMIS.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceLogs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.Vehicle;

public class VehicleModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
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

        new("View Vehicle Fuel & Odometer", "View", "Vehicle.FuelOdometer", IsBasic: true),
        new("Create Vehicle Fuel & Odometer", "Create", "Vehicle.FuelOdometer"),
        new("Update Vehicle Fuel & Odometer", "Update", "Vehicle.FuelOdometer"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(RegisteredPermissions);

        // QuestPDF community license
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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
        var fuelOdometerGroup = moduleGroup.MapGroup("/fuel-odometer");
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
        GenerateVehicleInventoryPdfEndpoint.Map(vehiclesGroup);

        // Fuel and odometer daily usage endpoints
        CreateVehicleDailyUsageEndpoint.Map(fuelOdometerGroup);
        UpdateVehicleDailyUsageEndpoint.Map(fuelOdometerGroup);
        SearchVehicleDailyUsageEndpoint.Map(fuelOdometerGroup);
        GetVehicleDailyUsageSummaryEndpoint.Map(fuelOdometerGroup);

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


