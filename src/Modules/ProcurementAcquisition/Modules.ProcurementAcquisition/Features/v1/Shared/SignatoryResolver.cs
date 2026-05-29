using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;

/// <summary>
/// Resolves the name printed in approval / certification signature blocks from the
/// <em>authenticated</em> identity rather than trusting a client-supplied value.
/// Prefers the MasterData employee full name (what appears on COA forms); falls back to
/// the authenticated username when the user has no employee profile. Never uses request input,
/// so a user holding the permission cannot sign under someone else's name.
/// </summary>
internal static class SignatoryResolver
{
    /// <summary>The signatory name + designation frozen at the moment of a signing action.</summary>
    public readonly record struct SignatoryInfo(string Name, string? Designation);

    public static async ValueTask<string> ResolveNameAsync(
        ICurrentUser currentUser, IMediator mediator, CancellationToken cancellationToken)
    {
        var info = await ResolveSignatoryAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);
        return info.Name;
    }

    /// <summary>
    /// Resolves both the name and designation (employee position) of the authenticated signer.
    /// Both are frozen onto the document so reprints stay faithful to who signed and their title
    /// at the time of their action. Falls back to the authenticated username when the user has no
    /// employee profile; designation is null in that case.
    /// </summary>
    public static async ValueTask<SignatoryInfo> ResolveSignatoryAsync(
        ICurrentUser currentUser, IMediator mediator, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(mediator);

        var identityUserId = currentUser.GetUserId().ToString();
        var employee = await mediator
            .Send(new GetEmployeeReferenceByIdentityUserIdQuery(identityUserId), cancellationToken)
            .ConfigureAwait(false);

        if (employee is not null)
        {
            var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                return new SignatoryInfo(fullName, employee.PositionName);
        }

        return new SignatoryInfo(currentUser.Name ?? string.Empty, null);
    }

    /// <summary>
    /// Resolves the name + designation of a <em>selected</em> employee (e.g. the PR requester, who is
    /// not necessarily the authenticated user) so both can be frozen onto the document at create time.
    /// </summary>
    public static async ValueTask<SignatoryInfo> ResolveByEmployeeIdAsync(
        Guid employeeId, IMediator mediator, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        var employee = await mediator
            .Send(new GetEmployeeReferenceByIdQuery(employeeId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Employee '{employeeId}' not found.");

        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        return new SignatoryInfo(fullName, employee.PositionName);
    }
}
