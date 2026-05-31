using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.QuestPdfReporting.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintEmployeeIssuance;

internal static class PrintEmployeeIssuanceEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/employee-issuance/pdf", Print)
            .WithName("QuestPdfReporting_PrintEmployeeIssuance")
            .WithSummary("Generate a QuestPDF for the Employee Issuance History report")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(QuestPdfReportingPermissions.ViewExpenditureReports);

    // ?pageWidth=a4|legal|longbond|letter   (default a4)
    // ?orientation=landscape|portrait        (default landscape)
    private static async Task<IResult> Print(
        IMediator mediator,
        CancellationToken ct,
        string?         employeeId  = null,
        DateTimeOffset? from        = null,
        DateTimeOffset? to          = null,
        string?         pageWidth   = null,
        string?         orientation = null)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";

        var bytes = await mediator.Send(
            new PrintEmployeeIssuanceQuery(employeeId, from, to, paperSize, orient), ct);
        return TypedResults.File(bytes, "application/pdf", "EmployeeIssuanceHistory.pdf");
    }
}
