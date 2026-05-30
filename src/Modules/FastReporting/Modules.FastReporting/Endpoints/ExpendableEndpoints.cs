using AMIS.Modules.FastReporting.Features.v1.Expendable.PrintRISFast;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Endpoints;

/// <summary>
/// FastReport endpoints for the Expendable area.
/// Add a new report here as a one-liner: <c>group.MapGroup("...").MapXxx();</c>
/// </summary>
internal static class ExpendableEndpoints
{
    public static IEndpointRouteBuilder MapExpendableFastReports(this IEndpointRouteBuilder moduleGroup)
    {
        var expendable = moduleGroup.MapGroup("/expendable");

        expendable.MapGroup("/supply-requests").MapRISFastReports();

        return moduleGroup;
    }

    private static IEndpointRouteBuilder MapRISFastReports(this IEndpointRouteBuilder group)
    {
        PrintRISFastEndpoint.Map(group);
        return group;
    }
}
