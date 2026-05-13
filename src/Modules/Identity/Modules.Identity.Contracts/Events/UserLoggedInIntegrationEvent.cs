using AMIS.Framework.Eventing.Abstractions;

namespace AMIS.Modules.Identity.Contracts.Events;

/// <summary>
/// Integration event raised when a user successfully logs in.
/// Other modules can subscribe to auto-link records (e.g. employee profiles).
/// </summary>
public sealed record UserLoggedInIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    string UserId,
    string Email,
    string FirstName,
    string LastName)
    : IIntegrationEvent;

