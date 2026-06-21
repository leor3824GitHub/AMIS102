using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.SignedDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.SignedDocuments.DownloadSignedDocument;

public static class DownloadSignedDocumentEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{documentType}/{documentId:guid}/download", Download)
            .WithName("BudgetDisbursement_DownloadSignedDocument")
            .WithSummary("Download the verified signed copy of a Budget Disbursement document of record")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(BudgetDisbursementPermissions.SignedDocuments.View);

    private static async Task<IResult> Download(
        BudgetDisbursementDocumentType documentType,
        Guid documentId,
        IMediator mediator,
        CancellationToken ct)
    {
        var file = await mediator.Send(new DownloadSignedDocumentQuery(documentType, documentId), ct);
        return file is null
            ? TypedResults.NotFound()
            : Results.File(file.Content, file.ContentType, file.FileName);
    }
}