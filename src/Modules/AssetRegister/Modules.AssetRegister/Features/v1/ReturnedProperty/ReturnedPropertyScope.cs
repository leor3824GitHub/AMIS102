using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Features.v1.Shared;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.ReturnedProperty;

/// <summary>
/// Decides whose return receipts a caller may see. The returned-property list is a shared worklist:
/// inspectors and custodians (operations roles) see everything, while a plain requester is hard-scoped
/// to their own requests — a client-supplied requester id is honoured only for privileged callers.
/// This is the server-side guarantee behind the "Mine / All" toggle on the page.
/// </summary>
internal static class ReturnedPropertyScope
{
    /// <summary>
    /// Resolves the requester id to filter the receipt list by:
    /// <list type="bullet">
    /// <item><c>null</c> — caller is privileged (inspector/custodian) and did not ask to filter → see all.</item>
    /// <item>a value — filter to that requester. For a privileged caller this is their chosen
    /// <paramref name="requestedFilter"/>; for a non-privileged caller it is always their own resolved
    /// employee id (or <see cref="Guid.Empty"/> when no employee profile is linked, which matches no
    /// receipt so they see nothing rather than everything).</item>
    /// </list>
    /// </summary>
    public static async ValueTask<Guid?> ResolveRequesterFilterAsync(
        ICurrentUser currentUser, IMediator mediator, Guid? requestedFilter, CancellationToken cancellationToken)
    {
        var privileged = PermissionClaims.HasAny(currentUser,
            AssetRegisterPermissions.ReturnedProperty.Inspect,
            AssetRegisterPermissions.ReturnedProperty.Accept);

        if (privileged)
            return requestedFilter is { } rid && rid != Guid.Empty ? rid : null;

        var employee = await CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);
        return employee?.Id ?? Guid.Empty;
    }

    /// <summary>
    /// Guards a requester-owned action on a return request (withdraw, reassign inspector): permitted for the
    /// person who raised it, or a property custodian (Accept) for administrative cleanup. Throws
    /// <see cref="ForbiddenException"/> otherwise. The actor is resolved from the authenticated user, never the
    /// payload. <paramref name="action"/> completes the sentence "…can {action}." (e.g. "withdraw it").
    /// </summary>
    public static async ValueTask EnsureCanActAsRequesterAsync(
        ICurrentUser currentUser, IMediator mediator, Guid requesterEmployeeId, string action, CancellationToken cancellationToken)
    {
        if (PermissionClaims.HasAny(currentUser, AssetRegisterPermissions.ReturnedProperty.Accept))
            return;

        var employee = await CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);
        if (employee is null || employee.Id != requesterEmployeeId)
            throw new ForbiddenException($"Only the person who requested this return, or a property custodian, can {action}.");
    }
}
