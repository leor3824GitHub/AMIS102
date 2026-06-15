using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintAccountability;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintCountAnnexes;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintIncident;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPhysicalCountReport;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertyCard;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRegSpi;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintUnserviceable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Endpoints;

internal static class AssetRegisterEndpoints
{
    internal static IEndpointRouteBuilder MapAssetRegisterQuestPdfReports(this IEndpointRouteBuilder group)
    {
        var assetRegister = group.MapGroup("asset-register");

        PrintCountAnnexesEndpoint.Map(assetRegister);
        PrintPhysicalCountReportEndpoint.Map(assetRegister);
        PrintRegSpiEndpoint.Map(assetRegister);
        PrintAccountabilityEndpoint.Map(assetRegister);
        PrintUnserviceableEndpoint.Map(assetRegister);
        PrintIncidentEndpoint.Map(assetRegister);
        PrintPropertyCardEndpoint.Map(assetRegister);

        return group;
    }
}
