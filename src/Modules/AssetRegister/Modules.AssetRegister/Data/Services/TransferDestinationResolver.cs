using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Multitenancy.Contracts;
using Mediator;

namespace AMIS.Modules.AssetRegister.Data.Services;

/// <summary>
/// Works out which agency a recipient employee belongs to, so an inter-agency transfer does not have to be
/// routed by hand.
/// <para>
/// The chain is <c>EmployeeProfile.OfficeId</c> → the tenant whose <c>AppTenantInfo.OfficeId</c> matches.
/// Employees are deliberately shared across tenants (<c>EmployeeProfile</c> is not <c>.IsMultiTenant()</c>),
/// so the recipient picker on a PPEIR already surfaces people from every agency — this turns that pick into
/// a destination. Routing matches on <c>OfficeId</c> only; the office <i>code</i> stored on the tenant is a
/// display snapshot and is never consulted here.
/// </para>
/// <para>
/// Returning null is the normal case, not an error: a recipient with no employee row, an office no tenant
/// claims, or a colleague in the sender's own agency all mean "no linked transfer" — the report is still
/// posted, it just falls back to the manual paper handshake.
/// </para>
/// </summary>
public sealed class TransferDestinationResolver(IMediator mediator, ITenantService tenantService)
{
    /// <summary>Resolves the agency a recipient employee belongs to, or null when there isn't one.</summary>
    /// <param name="employeeId">The recipient. <see cref="Guid.Empty"/> means a hand-typed name.</param>
    /// <param name="currentTenantId">The sending tenant, excluded so an internal issuance never looks like a transfer.</param>
    /// <param name="cancellationToken">Cancels the employee and tenant lookups.</param>
    public async Task<TransferDestinationDto?> ResolveForEmployeeAsync(
        Guid employeeId, string currentTenantId, CancellationToken cancellationToken)
    {
        if (employeeId == Guid.Empty)
        {
            return null;
        }

        var employee = await mediator
            .Send(new GetEmployeeReferenceByIdQuery(employeeId), cancellationToken)
            .ConfigureAwait(false);

        if (employee is null || employee.OfficeId == Guid.Empty)
        {
            return null;
        }

        var tenant = await tenantService
            .FindByOfficeIdAsync(employee.OfficeId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null || !tenant.IsActive)
        {
            return null;
        }

        // A recipient in our own agency is an ordinary internal issuance, not an inter-agency transfer.
        if (string.Equals(tenant.Identifier, currentTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new TransferDestinationDto(tenant.Identifier!, tenant.Name ?? tenant.Identifier!);
    }
}
