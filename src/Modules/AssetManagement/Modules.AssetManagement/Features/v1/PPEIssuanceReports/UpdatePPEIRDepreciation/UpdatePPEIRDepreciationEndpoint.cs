using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.PPEIssuanceReports.UpdatePPEIRDepreciation;

public static class UpdatePPEIRDepreciationEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{ppeirId:guid}/depreciation", async (
            Guid ppeirId,
            [FromBody] UpdatePPEIRDepreciationRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new UpdatePPEIRDepreciationCommand(ppeirId, request.Items);
            var result = await mediator.Send(command, ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(UpdatePPEIRDepreciationCommand))
        .WithSummary("Record accumulated depreciation and book value for PPEIR items (ASD/F.O. Accounting Unit)")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PPEIssuanceReports.Update);
}

public sealed record UpdatePPEIRDepreciationRequest(IReadOnlyList<PPEIRItemDepreciationRequest> Items);

