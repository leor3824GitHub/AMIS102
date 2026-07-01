using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRspi;

internal static class PrintRspiEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/rspi/pdf",
            (IMediator mediator, CancellationToken ct,
             DateOnly? dateFrom, DateOnly? dateTo, AssetType? assetType, bool? activeOnly,
             string? pageWidth, string? orientation, double? marginMm) =>
                PrintAsync(dateFrom, dateTo, assetType, activeOnly, mediator, pageWidth, orientation, marginMm, ct))
            .WithName("QuestPdfReporting_PrintRspi")
            .WithSummary("Generate the RSPI (Report of Semi-Expendable Property Issued) PDF")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Accountability.View);
    }

    private static async Task<IResult> PrintAsync(
        DateOnly? dateFrom, DateOnly? dateTo, AssetType? assetType, bool? activeOnly, IMediator mediator,
        string? pageWidth, string? orientation, double? marginMm, CancellationToken ct)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";
        var margin = marginMm is > 0 ? marginMm.Value : 12d;

        var bytes = await mediator.Send(
            new PrintRspiQuery(dateFrom, dateTo, assetType, activeOnly ?? true, paperSize, orient, margin), ct);
        return TypedResults.File(bytes, "application/pdf", "RSPI.pdf");
    }
}
