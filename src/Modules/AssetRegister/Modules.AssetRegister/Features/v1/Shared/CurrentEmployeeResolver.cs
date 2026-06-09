using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Shared;

/// <summary>
/// Resolves the <em>authenticated</em> user's employee profile from their identity, never from
/// client-supplied input. This is the single junction point for all employee self-service
/// ("My ...") endpoints in AssetRegister: a user holding a <c>MyAccountability.*</c> permission
/// can only ever act on records tied to their own resolved <see cref="EmployeeReferenceDto.Id"/>.
/// Mirrors <c>ProcurementAcquisition.Features.v1.Shared.SignatoryResolver</c>.
/// </summary>
internal static class CurrentEmployeeResolver
{
    /// <summary>
    /// Returns the current user's employee reference, or <c>null</c> when the user is
    /// unauthenticated or has no linked employee profile (callers map null to 404 / empty result).
    /// </summary>
    public static async ValueTask<EmployeeReferenceDto?> TryResolveAsync(
        ICurrentUser currentUser, IMediator mediator, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(mediator);

        if (!currentUser.IsAuthenticated())
            return null;

        var identityUserId = currentUser.GetUserId().ToString();
        if (string.IsNullOrWhiteSpace(identityUserId) || identityUserId == Guid.Empty.ToString())
            return null;

        return await mediator
            .Send(new GetEmployeeReferenceByIdentityUserIdQuery(identityUserId), cancellationToken)
            .ConfigureAwait(false);
    }
}
