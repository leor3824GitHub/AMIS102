using Mediator;

namespace AMIS.Modules.Multitenancy.Contracts.v1.CreateTenant;

public sealed record CreateTenantCommand(
    string Id,
    string Name,
    string? ConnectionString,
    string AdminEmail,
    string? Issuer,
    // The MasterData office this agency is. Optional — an unlinked tenant works exactly as before, it just
    // cannot be resolved as an auto-derived destination for an inter-agency property transfer.
    Guid? OfficeId = null,
    // Display-only snapshot of the office's code, for the tenant admin list. Multitenancy is a core module
    // and deliberately does not reference MasterData, so this comes from the caller. It can never affect
    // behaviour: transfer routing always matches on OfficeId.
    string? OfficeCode = null) : ICommand<CreateTenantCommandResponse>;
