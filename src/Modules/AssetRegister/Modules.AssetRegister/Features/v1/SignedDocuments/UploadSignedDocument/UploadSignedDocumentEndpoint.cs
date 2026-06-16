using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.SignedDocuments.UploadSignedDocument;

public static class UploadSignedDocumentEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Upload)
            .WithModuleName<UploadSignedDocumentCommand>()
            .WithSummary("Upload the scanned wet-signed copy of an Asset Register document of record (RRSP / RRP)")
            .DisableAntiforgery()
            .Produces<SignedDocumentDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.SignedDocuments.Upload);

    private static async Task<IResult> Upload(
        [FromForm] AssetRegisterDocumentType documentType,
        [FromForm] Guid documentId,
        IFormFile file,
        IMediator mediator,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return TypedResults.BadRequest("No file uploaded.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var result = await mediator.Send(new UploadSignedDocumentCommand(
            documentType, documentId, file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType,
            ms.ToArray()), ct);

        return TypedResults.Ok(result);
    }
}
