using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.Permissions;
using AMIS.Modules.Finance.Contracts.v1.SignedDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Finance.Features.v1.SignedDocuments.GetSignedDocument;

public static class GetSignedDocumentEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{documentType}/{documentId:guid}", GetMetadata)
            .WithName("Finance_GetSignedDocument")
            .WithSummary("Get metadata for the uploaded signed copy of a Finance document (or 404 if none)")
            .Produces<SignedDocumentDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(FinancePermissions.SignedDocuments.View);

    private static async Task<IResult> GetMetadata(
        FinanceDocumentType documentType,
        Guid documentId,
        IMediator mediator,
        CancellationToken ct)
    {
        var dto = await mediator.Send(new GetSignedDocumentQuery(documentType, documentId), ct);
        return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
    }
}
