using AMIS.Modules.QuestPdfReporting.Features.v1.AssetManagement.PrintRegSPI;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetManagement.PrintRSPI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Endpoints;

internal static class AssetManagementEndpoints
{
    internal static IEndpointRouteBuilder MapAssetManagementQuestPdfReports(this IEndpointRouteBuilder group)
    {
        var assetMgmt = group.MapGroup("asset-management");

        PrintRSPIEndpoint.Map(assetMgmt);
        PrintRegSPIEndpoint.Map(assetMgmt);

        return group;
    }
}
