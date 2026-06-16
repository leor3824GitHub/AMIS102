using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.SignedDocuments.GetSignedDocument;

public static class GetSignedDocumentEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{documentType}/{documentId:guid}", GetMetadata)
            .WithModuleName<GetSignedDocumentQuery>()
            .WithSummary("Get metadata for the uploaded signed copy of an Asset Register document (or 404 if none)")
            .Produces<SignedDocumentDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.SignedDocuments.View);

    private static async Task<IResult> GetMetadata(
        AssetRegisterDocumentType documentType,
        Guid documentId,
        IMediator mediator,
        CancellationToken ct)
    {
        var dto = await mediator.Send(new GetSignedDocumentQuery(documentType, documentId), ct);
        return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
    }
}
