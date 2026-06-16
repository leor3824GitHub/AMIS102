using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.SignedDocuments.DownloadSignedDocument;

public static class DownloadSignedDocumentEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{documentType}/{documentId:guid}/download", Download)
            .WithModuleName<DownloadSignedDocumentQuery>()
            .WithSummary("Download the verified signed copy of an Asset Register document of record")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.SignedDocuments.View);

    private static async Task<IResult> Download(
        AssetRegisterDocumentType documentType,
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
