using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.Permissions;
using AMIS.Modules.Finance.Contracts.v1.SignedDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Finance.Features.v1.SignedDocuments.UploadSignedDocument;

public static class UploadSignedDocumentEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Upload)
            .WithName("Finance_UploadSignedDocument")
            .WithSummary("Upload the scanned wet-signed copy of a Finance document of record (e.g. a DV)")
            .DisableAntiforgery()
            .Produces<SignedDocumentDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(FinancePermissions.SignedDocuments.Upload);

    private static async Task<IResult> Upload(
        [FromForm] FinanceDocumentType documentType,
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
