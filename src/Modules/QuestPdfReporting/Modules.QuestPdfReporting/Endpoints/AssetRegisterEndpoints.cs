using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintCountAnnexes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Endpoints;

internal static class AssetRegisterEndpoints
{
    internal static IEndpointRouteBuilder MapAssetRegisterQuestPdfReports(this IEndpointRouteBuilder group)
    {
        var assetRegister = group.MapGroup("asset-register");

        assetRegister.Map();

        return group;
    }
}
