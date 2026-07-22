using Mediator;

namespace AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;

/// <param name="AnnexECode">
/// The agency's office code, used in property code generation and to stamp record ownership. Derived from
/// the office the tenant is linked to — read-only to clients.
/// </param>
/// <param name="OfficeId">The linked office, or null when no administrator has linked this tenant yet.</param>
/// <param name="OfficeName">Display name of the linked office. Resolved per request, never stored.</param>
public sealed record OrganizationProfileDto(
    Guid Id,
    string Name,
    string? ShortName,
    string? Address,
    string? LogoUrl,
    string? AnnexECode,
    Guid? ApprovingOfficialId = null,
    string? ApprovingOfficialName = null,
    string? ApprovingOfficialDesignation = null,
    Guid? AssistantRegionalManagerId = null,
    string? AssistantRegionalManagerName = null,
    string? AssistantRegionalManagerDesignation = null,
    Guid? AccountantId = null,
    string? AccountantName = null,
    string? AccountantDesignation = null,
    Guid? SupervisingAdminOfficerId = null,
    string? SupervisingAdminOfficerName = null,
    string? SupervisingAdminOfficerDesignation = null,
    Guid? BudgetOfficerId = null,
    string? BudgetOfficerName = null,
    string? BudgetOfficerDesignation = null,
    Guid? PropertyCustodianId = null,
    string? PropertyCustodianName = null,
    string? PropertyCustodianDesignation = null,
    Guid? OfficeId = null,
    string? OfficeName = null);

public sealed record GetOrganizationProfileQuery() : IQuery<OrganizationProfileDto?>;

/// <summary>
/// Note there is no agency office code here: it is derived server-side from the office the tenant is linked
/// to (Tenants page), so the profile can never point at a different office than transfers are routed by.
/// </summary>
public sealed record UpsertOrganizationProfileCommand(
    string Name,
    string? ShortName,
    string? Address,
    string? LogoUrl,
    Guid? ApprovingOfficialId = null,
    string? ApprovingOfficialName = null,
    string? ApprovingOfficialDesignation = null,
    Guid? AssistantRegionalManagerId = null,
    string? AssistantRegionalManagerName = null,
    string? AssistantRegionalManagerDesignation = null,
    Guid? AccountantId = null,
    string? AccountantName = null,
    string? AccountantDesignation = null,
    Guid? SupervisingAdminOfficerId = null,
    string? SupervisingAdminOfficerName = null,
    string? SupervisingAdminOfficerDesignation = null,
    Guid? BudgetOfficerId = null,
    string? BudgetOfficerName = null,
    string? BudgetOfficerDesignation = null,
    Guid? PropertyCustodianId = null,
    string? PropertyCustodianName = null,
    string? PropertyCustodianDesignation = null) : ICommand<OrganizationProfileDto>;

