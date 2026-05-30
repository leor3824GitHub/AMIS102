using AMIS.Modules.FastReporting.Features.v1.Vehicles.PrintVehicleInventoryFast;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Endpoints;

/// <summary>
/// FastReport endpoints for the Vehicle area.
/// Add a new report here as a one-liner: <c>group.MapGroup("...").MapXxx();</c>
/// </summary>
internal static class VehicleEndpoints
{
    public static IEndpointRouteBuilder MapVehicleFastReports(this IEndpointRouteBuilder moduleGroup)
    {
        var vehicle = moduleGroup.MapGroup("/vehicle");

        vehicle.MapGroup("/inventory").MapVehicleInventoryFastReports();

        return moduleGroup;
    }

    private static IEndpointRouteBuilder MapVehicleInventoryFastReports(this IEndpointRouteBuilder group)
    {
        PrintVehicleInventoryFastEndpoint.Map(group);
        return group;
    }
}
