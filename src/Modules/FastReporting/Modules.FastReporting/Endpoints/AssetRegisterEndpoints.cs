using AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintPPEReceivingReportFast;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Endpoints;

/// <summary>
/// FastReport endpoints for the AssetRegister area. One extension method per area keeps
/// <c>FastReportingModule.MapEndpoints</c> a flat registry as the number of reports grows.
/// Add a new report here as a one-liner: <c>group.MapGroup("...").MapXxx();</c>
/// </summary>
internal static class AssetRegisterEndpoints
{
    public static IEndpointRouteBuilder MapAssetRegisterFastReports(this IEndpointRouteBuilder moduleGroup)
    {
        var assetRegister = moduleGroup.MapGroup("/asset-register");

        assetRegister.MapGroup("/receiving-reports").MapPPEReceivingReportFastReports();

        return moduleGroup;
    }

    private static IEndpointRouteBuilder MapPPEReceivingReportFastReports(this IEndpointRouteBuilder group)
    {
        PrintPPEReceivingReportFastEndpoint.Map(group);
        return group;
    }
}
