using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRegSpi;

internal static class PrintRegSpiEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/regspi/pdf",
            (IMediator mediator, CancellationToken ct,
             DateOnly? asOfDate, AssetType? assetType, Guid? custodianId, string? fundCluster, string? propertyClass,
             string? pageWidth, string? orientation, double? marginMm) =>
                PrintAsync(asOfDate, assetType, custodianId, fundCluster, propertyClass, mediator, pageWidth, orientation, marginMm, ct))
            .WithName("QuestPdfReporting_PrintRegSpi")
            .WithSummary("Generate the RegSPI (Registry of Semi-Expendable Property Issued) PDF")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Accountability.View);
    }

    private static async Task<IResult> PrintAsync(
        DateOnly? asOfDate, AssetType? assetType, Guid? custodianId, string? fundCluster, string? propertyClass, IMediator mediator,
        string? pageWidth, string? orientation, double? marginMm, CancellationToken ct)
    {
        // Annex A.4 has 15 leaf columns — legal landscape is the default fit.
        var paperSize = (pageWidth ?? "legal").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";
        var margin = marginMm is > 0 ? marginMm.Value : 12d;

        var bytes = await mediator.Send(new PrintRegSpiQuery(asOfDate, assetType, custodianId, fundCluster, propertyClass, paperSize, orient, margin), ct);
        return TypedResults.File(bytes, "application/pdf", "RegSPI.pdf");
    }
}
