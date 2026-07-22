using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Multitenancy.Contracts.v1.UpdateTenantOffice;
using Mediator;

namespace AMIS.Modules.Multitenancy.Features.v1.UpdateTenantOffice;

public sealed class UpdateTenantOfficeCommandHandler(ITenantService tenantService)
    : ICommandHandler<UpdateTenantOfficeCommand, UpdateTenantOfficeCommandResponse>
{
    public async ValueTask<UpdateTenantOfficeCommandResponse> Handle(
        UpdateTenantOfficeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = await tenantService
            .LinkOfficeAsync(command.TenantId, command.OfficeId, command.OfficeCode, cancellationToken)
            .ConfigureAwait(false);

        return new UpdateTenantOfficeCommandResponse(tenant.Id, command.OfficeId, tenant.OfficeCode);
    }
}
