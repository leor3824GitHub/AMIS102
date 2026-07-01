using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPtr;

internal static class PrintPtrEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/issuance/{reportId:guid}/ptr/pdf",
            (Guid reportId, IMediator mediator, CancellationToken ct,
             string? pageWidth, string? orientation, double? marginMm) =>
                PrintAsync(reportId, mediator, pageWidth, orientation, marginMm, ct))
            .WithName("QuestPdfReporting_PrintPtr")
            .WithSummary("Generate the PTR (Property Transfer Report) PDF from a PPEIR")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.Issuance.View);
    }

    private static async Task<IResult> PrintAsync(
        Guid reportId, IMediator mediator,
        string? pageWidth, string? orientation, double? marginMm, CancellationToken ct)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape" ? "landscape" : "portrait";
        var margin = marginMm is > 0 ? marginMm.Value : 12d;

        var bytes = await mediator.Send(new PrintPtrQuery(reportId, paperSize, orient, margin), ct);
        return TypedResults.File(bytes, "application/pdf", "PTR.pdf");
    }
}
