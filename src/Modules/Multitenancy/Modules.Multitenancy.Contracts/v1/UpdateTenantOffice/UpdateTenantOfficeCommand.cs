using Mediator;

namespace AMIS.Modules.Multitenancy.Contracts.v1.UpdateTenantOffice;

/// <summary>
/// Points a tenant at the MasterData office it represents. This is what makes the agency resolvable as a
/// destination for an inter-agency property transfer: a recipient employee's <c>OfficeId</c> is matched
/// against every tenant's <c>OfficeId</c> to work out which agency's books the assets are headed for.
/// <para>
/// Needed as its own command because tenants created before the link existed have a null
/// <c>OfficeId</c> and would otherwise have to be recreated.
/// </para>
/// </summary>
public sealed record UpdateTenantOfficeCommand(
    string TenantId,
    Guid OfficeId,
    // Display-only snapshot for the tenant admin list; never used for matching.
    string? OfficeCode = null) : ICommand<UpdateTenantOfficeCommandResponse>;

public sealed record UpdateTenantOfficeCommandResponse(string TenantId, Guid OfficeId, string? OfficeCode);
