using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.Permissions;
using AMIS.Modules.Finance.Contracts.v1.SignedDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Finance.Features.v1.SignedDocuments.DownloadSignedDocument;

public static class DownloadSignedDocumentEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{documentType}/{documentId:guid}/download", Download)
            .WithName("Finance_DownloadSignedDocument")
            .WithSummary("Download the verified signed copy of a Finance document of record")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(FinancePermissions.SignedDocuments.View);

    private static async Task<IResult> Download(
        FinanceDocumentType documentType,
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
