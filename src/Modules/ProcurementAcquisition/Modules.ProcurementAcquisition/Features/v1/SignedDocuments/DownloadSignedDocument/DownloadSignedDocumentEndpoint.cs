using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.SignedDocuments.DownloadSignedDocument;

public static class DownloadSignedDocumentEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{documentType}/{documentId:guid}/download", Download)
            .WithName("Procurement_DownloadSignedDocument")
            .WithSummary("Download the verified signed copy of a procurement document of record")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.SignedDocuments.View);

    private static async Task<IResult> Download(
        ProcurementDocumentType documentType,
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
