using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.SignedDocuments.GetSignedDocument;

public static class GetSignedDocumentEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{documentType}/{documentId:guid}", GetMetadata)
            .WithName("Procurement_GetSignedDocument")
            .WithSummary("Get metadata for the uploaded signed copy of a procurement document (or 404 if none)")
            .Produces<SignedDocumentDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.SignedDocuments.View);

    private static async Task<IResult> GetMetadata(
        ProcurementDocumentType documentType,
        Guid documentId,
        IMediator mediator,
        CancellationToken ct)
    {
        var dto = await mediator.Send(new GetSignedDocumentQuery(documentType, documentId), ct);
        return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
    }
}
