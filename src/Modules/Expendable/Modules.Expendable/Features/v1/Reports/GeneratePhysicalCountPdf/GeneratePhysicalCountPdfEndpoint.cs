using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Expendable.Contracts.v1.Reports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Expendable.Features.v1.Reports.GeneratePhysicalCountPdf;

public static class GeneratePhysicalCountPdfEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/physical-count/pdf", Generate)
            .WithName(nameof(GeneratePhysicalCountPdfCommand))
            .WithSummary("Generate a PDF for the Physical Count report")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.ViewReports);

    private static async Task<IResult> Generate(
        GeneratePhysicalCountPdfCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var bytes = await mediator.Send(command, cancellationToken);
        return TypedResults.File(bytes, "application/pdf", "PhysicalCountReport.pdf");
    }
}
