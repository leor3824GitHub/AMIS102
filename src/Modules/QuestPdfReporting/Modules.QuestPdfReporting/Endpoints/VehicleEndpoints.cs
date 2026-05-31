using AMIS.Modules.QuestPdfReporting.Features.v1.Vehicle.PrintVehicleInventory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Endpoints;

internal static class VehicleEndpoints
{
    internal static IEndpointRouteBuilder MapVehicleQuestPdfReports(this IEndpointRouteBuilder group)
    {
        var vehicle = group.MapGroup("vehicle");

        PrintVehicleInventoryEndpoint.Map(vehicle);

        return group;
    }
}
