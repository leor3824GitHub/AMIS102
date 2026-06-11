using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintCountAnnexes;

internal static class PrintCountAnnexesEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/physical-count/{sessionId:guid}/annex-b/pdf",
            (Guid sessionId, IMediator mediator, CancellationToken ct,
             string? pageWidth, string? orientation, double? marginMm) =>
                PrintAsync(sessionId, CountAnnexKind.FoundAtStation, mediator, pageWidth, orientation, marginMm, ct))
            .WithName("QuestPdfReporting_PrintCountAnnexB")
            .WithSummary("Generate Annex B (List of PPE Found at Station) PDF for a physical count session")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Count.View);

        endpoints.MapGet("/physical-count/{sessionId:guid}/annex-c/pdf",
            (Guid sessionId, IMediator mediator, CancellationToken ct,
             string? pageWidth, string? orientation, double? marginMm) =>
                PrintAsync(sessionId, CountAnnexKind.NonExistingMissing, mediator, pageWidth, orientation, marginMm, ct))
            .WithName("QuestPdfReporting_PrintCountAnnexC")
            .WithSummary("Generate Annex C (List of Non-Existing/Missing PPE) PDF for a physical count session")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Count.View);
    }

    // ?pageWidth=a4|legal|longbond|letter (default a4)  ?orientation=portrait|landscape (default portrait)
    // ?marginMm=<page margin in mm> (default 15)
    private static async Task<IResult> PrintAsync(
        Guid sessionId, CountAnnexKind annex, IMediator mediator,
        string? pageWidth, string? orientation, double? marginMm, CancellationToken ct)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape" ? "landscape" : "portrait";
        var margin = marginMm is > 0 ? marginMm.Value : 15d;

        var bytes = await mediator.Send(new PrintCountAnnexesQuery(sessionId, annex, paperSize, orient, margin), ct);
        var name = annex == CountAnnexKind.FoundAtStation ? "AnnexB-FoundAtStation" : "AnnexC-MissingPPE";
        return TypedResults.File(bytes, "application/pdf", $"{name}.pdf");
    }
}
