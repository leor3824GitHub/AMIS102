using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Reporting;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.PrintPurchaseRequest;

public static class PrintPurchaseRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/print", PrintPurchaseRequest)
            .WithName("Procurement_PrintPurchaseRequest")
            .WithSummary("Generate a PDF for a purchase request")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.PurchaseRequests.View);

    private static async Task<IResult> PrintPurchaseRequest(
        Guid id,
        IMediator mediator,
        CancellationToken ct,
        string? pageWidth  = null,
        string? pageHeight = null)
    {
        // Convenience: accept shorthand names for common paper sizes
        (pageWidth, pageHeight) = pageWidth?.ToLowerInvariant() switch
        {
            "a4"        => GovernmentPaperSizes.A4,
            "legal"     => GovernmentPaperSizes.Legal,
            "longbond"  => GovernmentPaperSizes.LongBond,
            "halfa4"    => GovernmentPaperSizes.HalfA4,
            "halflegal" => GovernmentPaperSizes.HalfLegal,
            "halflong"  => GovernmentPaperSizes.HalfLong,
            _           => (pageWidth, pageHeight)
        };

        var bytes = await mediator.Send(
            new PrintPurchaseRequestQuery(id, pageWidth, pageHeight), ct);

        return Results.File(bytes, "application/pdf", $"PR-{id}.pdf");
    }
}
