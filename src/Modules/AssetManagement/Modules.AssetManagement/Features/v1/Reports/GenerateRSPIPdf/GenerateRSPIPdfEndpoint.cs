using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.GenerateRSPIPdf;

public static class GenerateRSPIPdfEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/rspi/pdf", Generate)
            .WithName(nameof(GenerateRSPIPdfCommand))
            .WithSummary("Generate a PDF for the Report of Semi-Expendable Property Issued (RSPI)")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetManagementModuleConstants.Permissions.Reports.View);

    private static async Task<IResult> Generate(
        GenerateRSPIPdfCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var bytes = await mediator.Send(command, cancellationToken);
        return TypedResults.File(bytes, "application/pdf", "RSPIReport.pdf");
    }
}

