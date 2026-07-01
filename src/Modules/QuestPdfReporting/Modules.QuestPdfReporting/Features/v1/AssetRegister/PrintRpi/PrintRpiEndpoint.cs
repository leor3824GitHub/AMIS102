using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRpi;

internal static class PrintRpiEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/rpi/pdf",
            (IMediator mediator, CancellationToken ct,
             DateOnly? dateFrom, DateOnly? dateTo,
             string? pageWidth, string? orientation, double? marginMm) =>
                PrintAsync(dateFrom, dateTo, mediator, pageWidth, orientation, marginMm, ct))
            .WithName("QuestPdfReporting_PrintRpi")
            .WithSummary("Generate the RPI (Report on Property Issued — PPE) PDF")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Accountability.View);
    }

    private static async Task<IResult> PrintAsync(
        DateOnly? dateFrom, DateOnly? dateTo, IMediator mediator,
        string? pageWidth, string? orientation, double? marginMm, CancellationToken ct)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";
        var margin = marginMm is > 0 ? marginMm.Value : 12d;

        var bytes = await mediator.Send(
            new PrintRpiQuery(dateFrom, dateTo, paperSize, orient, margin), ct);
        return TypedResults.File(bytes, "application/pdf", "RPI.pdf");
    }
}
