using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.UpdateIssuanceReportDepreciation;

public static class UpdateIssuanceReportDepreciationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{reportId:guid}/depreciation", Handle)
            .WithModuleName<UpdateIssuanceReportDepreciationCommand>()
            .WithSummary("Update accumulated depreciation and book value for PPEIR lines")
            .Produces<PropertyIssuanceReportDto>()
            .RequirePermission(AssetRegisterPermissions.Issuance.Update);

    private static async Task<IResult> Handle(
        Guid reportId,
        UpdateIssuanceReportDepreciationRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateIssuanceReportDepreciationCommand(reportId, request.Lines), ct);
        return TypedResults.Ok(result);
    }
}

public sealed record UpdateIssuanceReportDepreciationRequest(IReadOnlyList<LineDepreciationDto> Lines);
