using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Reports;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/accountability/{id:guid}", HandleAccountabilityReport)
            .WithName(nameof(GetAccountabilityReportQuery))
            .WithSummary("Generate accountability document view (ICS/PAR)")
            .Produces<AccountabilityReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Accountability.View);

        endpoints.MapGet("/issuance/{id:guid}", HandleIssuanceReport)
            .WithName(nameof(GetIssuanceReportDocumentQuery))
            .WithSummary("Generate issuance document view (RSPI/PPEIR)")
            .Produces<IssuanceReportDocumentDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Issuance.View);

        endpoints.MapGet("/count/{sessionId:guid}", HandlePhysicalCountReport)
            .WithName(nameof(GetPhysicalCountReportQuery))
            .WithSummary("Generate physical count report view (RPCSEMEX/RPCPPE)")
            .Produces<PhysicalCountReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Count.View);

        endpoints.MapGet("/regspi", HandleRegSpiReport)
            .WithName(nameof(GetRegSpiReportQuery))
            .WithSummary("Generate RegSPI document view")
            .Produces<RegSpiReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Accountability.View);

        endpoints.MapGet("/incidents/{id:guid}", HandleIncidentReport)
            .WithName(nameof(GetIncidentReportDocumentQuery))
            .WithSummary("Generate incident document view (RLSDDSP)")
            .Produces<IncidentReportDocumentDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.View);

        endpoints.MapGet("/unserviceable/{id:guid}", HandleUnserviceableReport)
            .WithName(nameof(GetUnserviceableReportDocumentQuery))
            .WithSummary("Generate unserviceable document view (IIRUSP/IIRUP)")
            .Produces<UnserviceableReportDocumentDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Unserviceable.View);

        return endpoints;
    }

    private static async Task<IResult> HandleAccountabilityReport(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAccountabilityReportQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> HandleIssuanceReport(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetIssuanceReportDocumentQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> HandlePhysicalCountReport(
        Guid sessionId,
        IMediator mediator,
        AssetType? assetType,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPhysicalCountReportQuery(sessionId, assetType), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> HandleRegSpiReport(
        IMediator mediator,
        DateOnly? asOfDate,
        AssetType? assetType,
        Guid? custodianId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRegSpiReportQuery(asOfDate, assetType, custodianId), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> HandleIncidentReport(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetIncidentReportDocumentQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> HandleUnserviceableReport(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUnserviceableReportDocumentQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
