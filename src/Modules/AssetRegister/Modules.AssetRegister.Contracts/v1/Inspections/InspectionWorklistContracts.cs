using AMIS.Framework.Shared.Inspections;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Inspections;

/// <summary>
/// Returns every Returned Property return-request currently awaiting the <em>calling</em> user's inspection.
/// Self-scoped: the caller's employee is resolved server-side from the authenticated identity, never
/// client-supplied. Backs the unified "My Inspections" worklist.
/// </summary>
public sealed record GetMyPendingReturnedPropertyInspectionsQuery
    : IQuery<IReadOnlyList<PendingInspectionItem>>;
