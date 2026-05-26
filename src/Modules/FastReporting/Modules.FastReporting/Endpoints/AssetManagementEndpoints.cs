using AMIS.Modules.FastReporting.Features.v1.AssetManagement.PrintICSFast;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Endpoints;

/// <summary>
/// FastReport endpoints for the AssetManagement area.
/// Add a new report here as a one-liner: <c>group.MapGroup("...").MapXxx();</c>
/// </summary>
internal static class AssetManagementEndpoints
{
    public static IEndpointRouteBuilder MapAssetManagementFastReports(this IEndpointRouteBuilder moduleGroup)
    {
        var assetManagement = moduleGroup.MapGroup("/asset-management");

        assetManagement.MapGroup("/inventory-custodian-slips").MapICSFastReports();

        return moduleGroup;
    }

    private static IEndpointRouteBuilder MapICSFastReports(this IEndpointRouteBuilder group)
    {
        PrintICSFastEndpoint.Map(group);
        return group;
    }
}
