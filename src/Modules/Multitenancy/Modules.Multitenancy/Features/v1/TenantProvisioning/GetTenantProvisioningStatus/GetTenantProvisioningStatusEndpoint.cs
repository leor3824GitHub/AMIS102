using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Multitenancy.Contracts.v1.TenantProvisioning;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Multitenancy.Features.v1.TenantProvisioning.GetTenantProvisioningStatus;

public static class GetTenantProvisioningStatusEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{tenantId}/provisioning", async (
            [FromRoute] string tenantId,
            [FromServices] IMediator mediator) =>
            await mediator.Send(new GetTenantProvisioningStatusQuery(tenantId)))
            .WithName("GetTenantProvisioningStatus")
            .WithSummary("Get tenant provisioning status")
            .RequirePermission(MultitenancyConstants.Permissions.View)
            .WithDescription("Get latest provisioning status for a tenant.");
    }
}

