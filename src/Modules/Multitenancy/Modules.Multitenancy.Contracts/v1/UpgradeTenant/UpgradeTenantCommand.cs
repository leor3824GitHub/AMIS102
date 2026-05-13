using Mediator;

namespace AMIS.Modules.Multitenancy.Contracts.v1.UpgradeTenant;

public sealed record UpgradeTenantCommand(string Tenant, DateTime ExtendedExpiryDate)
    : ICommand<UpgradeTenantCommandResponse>;
