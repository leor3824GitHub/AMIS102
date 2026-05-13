using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.v1.Reports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GenerateDepartmentIssuancePdf;

public static class GenerateDepartmentIssuancePdfEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/department-issuance/pdf", Generate)
            .WithName(nameof(GenerateDepartmentIssuancePdfCommand))
            .WithSummary("Generate a PDF for the Department Issuance report")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.ViewReports);

    private static async Task<IResult> Generate(
        GenerateDepartmentIssuancePdfCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var bytes = await mediator.Send(command, cancellationToken);
        return TypedResults.File(bytes, "application/pdf", "DepartmentIssuanceReport.pdf");
    }
}

