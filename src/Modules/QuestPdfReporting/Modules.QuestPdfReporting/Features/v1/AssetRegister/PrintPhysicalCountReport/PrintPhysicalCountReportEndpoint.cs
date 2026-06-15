using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPhysicalCountReport;

internal static class PrintPhysicalCountReportEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/physical-count/{sessionId:guid}/rpcppe/pdf",
            (Guid sessionId, IMediator mediator, CancellationToken ct,
             string? pageWidth, string? orientation, double? marginMm) =>
                PrintAsync(sessionId, AssetType.PPE, mediator, pageWidth, orientation, marginMm, ct))
            .WithName("QuestPdfReporting_PrintRPCPPE")
            .WithSummary("Generate the RPCPPE (Report on Physical Count of PPE) PDF for a count session")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Count.View);

        endpoints.MapGet("/physical-count/{sessionId:guid}/rpcsemex/pdf",
            (Guid sessionId, IMediator mediator, CancellationToken ct,
             string? pageWidth, string? orientation, double? marginMm) =>
                PrintAsync(sessionId, AssetType.SE, mediator, pageWidth, orientation, marginMm, ct))
            .WithName("QuestPdfReporting_PrintRPCSEMEX")
            .WithSummary("Generate the RPCSEMEX (Report on Physical Count of Semi-Expendable Property) PDF for a count session")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Count.View);
    }

    private static async Task<IResult> PrintAsync(
        Guid sessionId, AssetType assetType, IMediator mediator,
        string? pageWidth, string? orientation, double? marginMm, CancellationToken ct)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";
        var margin = marginMm is > 0 ? marginMm.Value : 12d;

        var bytes = await mediator.Send(new PrintPhysicalCountReportQuery(sessionId, assetType, paperSize, orient, margin), ct);
        var name = assetType == AssetType.PPE ? "RPCPPE" : "RPCSEMEX";
        return TypedResults.File(bytes, "application/pdf", $"{name}.pdf");
    }
}
