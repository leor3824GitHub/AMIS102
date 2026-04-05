using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Expendable.Contracts.v1.Reports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Expendable.Features.v1.Reports.GenerateEmployeeIssuancePdf;

public static class GenerateEmployeeIssuancePdfEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/employee-issuance/pdf", Generate)
            .WithName(nameof(GenerateEmployeeIssuancePdfCommand))
            .WithSummary("Generate a PDF for the Employee Issuance History report")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.ViewReports);

    private static async Task<IResult> Generate(
        GenerateEmployeeIssuancePdfCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var bytes = await mediator.Send(command, cancellationToken);
        return TypedResults.File(bytes, "application/pdf", "EmployeeIssuanceHistory.pdf");
    }
}
