using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.GenerateRegSPIPdf;

public static class GenerateRegSPIPdfEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/reg-spi/pdf", Generate)
            .WithName(nameof(GenerateRegSPIPdfCommand))
            .WithSummary("Generate a PDF for the Registry of Semi-Expendable Property Issued (RegSPI)")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetManagementModuleConstants.Permissions.Reports.View);

    private static async Task<IResult> Generate(
        GenerateRegSPIPdfCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var bytes = await mediator.Send(command, cancellationToken);
        return TypedResults.File(bytes, "application/pdf", "RegSPIReport.pdf");
    }
}

