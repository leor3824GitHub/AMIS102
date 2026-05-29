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
    public static async ValueTask<string> ResolveNameAsync(
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
                return fullName;
        }

        return currentUser.Name ?? string.Empty;
    }
}
