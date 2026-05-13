using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Multitenancy.Contracts.v1.CreateTenant;
using AMIS.Modules.Multitenancy.Provisioning;
using Mediator;

namespace AMIS.Modules.Multitenancy.Features.v1.CreateTenant;

public sealed class CreateTenantCommandHandler(ITenantService tenantService, ITenantProvisioningService provisioningService)
    : ICommandHandler<CreateTenantCommand, CreateTenantCommandResponse>
{
    public async ValueTask<CreateTenantCommandResponse> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantId = await tenantService.CreateAsync(
            command.Id,
            command.Name,
            command.ConnectionString,
            command.AdminEmail,
            command.Issuer,
            cancellationToken);

        var provisioning = await provisioningService.StartAsync(tenantId, cancellationToken);

        return new CreateTenantCommandResponse(
            tenantId,
            provisioning.CorrelationId,
            provisioning.Status.ToString());
    }
}

