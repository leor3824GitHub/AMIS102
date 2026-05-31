using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetManagement.Contracts.v1.Reports;
using AMIS.Modules.QuestPdfReporting.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetManagement.PrintRegSPI;

internal static class PrintRegSPIEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{employeeId:guid}/reg-spi/pdf", Print)
            .WithName("QuestPdfReporting_PrintRegSPI")
            .WithSummary("Generate a QuestPDF for the Registry of Semi-Expendable Property Issued (RegSPI)")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(QuestPdfReportingPermissions.ViewAssetReports);

    private static async Task<IResult> Print(
        Guid       employeeId,
        IMediator  mediator,
        CancellationToken ct,
        AssetType? assetType  = null,
        ICSStatus? status     = null,
        int        pageNumber = 1,
        int        pageSize   = 10000)
    {
        var bytes = await mediator.Send(
            new PrintRegSPIQuery(employeeId, assetType, status, pageNumber, pageSize), ct);
        return TypedResults.File(bytes, "application/pdf", "RegSPIReport.pdf");
    }
}
