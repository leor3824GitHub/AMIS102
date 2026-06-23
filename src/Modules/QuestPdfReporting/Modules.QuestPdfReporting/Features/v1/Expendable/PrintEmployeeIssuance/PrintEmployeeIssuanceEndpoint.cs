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
    // ?orientation=landscape|portrait        (default portrait)
    // ?marginMm=<page margin in millimetres>  (default 15)
    private static async Task<IResult> Print(
        IMediator mediator,
        CancellationToken ct,
        string?         employeeId  = null,
        DateTimeOffset? from        = null,
        DateTimeOffset? to          = null,
        string?         pageWidth   = null,
        string?         orientation = null,
        double?         marginMm    = null)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape" ? "landscape" : "portrait";
        var margin = marginMm is > 0 ? marginMm.Value : 15d;

        var bytes = await mediator.Send(
            new PrintEmployeeIssuanceQuery(employeeId, from, to, paperSize, orient, margin), ct);
        return TypedResults.File(bytes, "application/pdf", "EmployeeIssuanceHistory.pdf");
    }
}
