using AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintAccountabilityICSFast;
using AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintPPEIssuanceReportFast;
using AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintPPEReceivingReportFast;
using AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintSMIRFast;
using AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintSMRRFast;
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
        assetRegister.MapGroup("/issuance-reports").MapPPEIssuanceReportFastReports();
        assetRegister.MapGroup("/smir").MapSMIRFastReports();
        assetRegister.MapGroup("/smrr").MapSMRRFastReports();
        assetRegister.MapGroup("/accountabilities").MapAccountabilityICSFastReports();

        return moduleGroup;
    }

    private static IEndpointRouteBuilder MapPPEReceivingReportFastReports(this IEndpointRouteBuilder group)
    {
        PrintPPEReceivingReportFastEndpoint.Map(group);
        return group;
    }

    private static IEndpointRouteBuilder MapPPEIssuanceReportFastReports(this IEndpointRouteBuilder group)
    {
        PrintPPEIssuanceReportFastEndpoint.Map(group);
        return group;
    }

    private static IEndpointRouteBuilder MapSMIRFastReports(this IEndpointRouteBuilder group)
    {
        PrintSMIRFastEndpoint.Map(group);
        return group;
    }

    private static IEndpointRouteBuilder MapSMRRFastReports(this IEndpointRouteBuilder group)
    {
        PrintSMRRFastEndpoint.Map(group);
        return group;
    }

    private static IEndpointRouteBuilder MapAccountabilityICSFastReports(this IEndpointRouteBuilder group)
    {
        PrintAccountabilityICSFastEndpoint.Map(group);
        return group;
    }
}
