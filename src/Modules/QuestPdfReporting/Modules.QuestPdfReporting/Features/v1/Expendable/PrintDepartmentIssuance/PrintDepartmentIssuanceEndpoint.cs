using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.QuestPdfReporting.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintDepartmentIssuance;

internal static class PrintDepartmentIssuanceEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/department-issuance/pdf", Print)
            .WithName("QuestPdfReporting_PrintDepartmentIssuance")
            .WithSummary("Generate a QuestPDF for the Department Issuance report")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(QuestPdfReportingPermissions.ViewExpenditureReports);

    private static async Task<IResult> Print(
        IMediator mediator,
        CancellationToken ct,
        string?         departmentId = null,
        DateTimeOffset? from         = null,
        DateTimeOffset? to           = null)
    {
        var bytes = await mediator.Send(new PrintDepartmentIssuanceQuery(departmentId, from, to), ct);
        return TypedResults.File(bytes, "application/pdf", "DepartmentIssuanceReport.pdf");
    }
}
