using AMIS.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace AMIS.Modules.Multitenancy.Contracts.v1.TenantProvisioning;

public sealed record RetryTenantProvisioningCommand(string TenantId) : ICommand<TenantProvisioningStatusDto>;

