using Mediator;

namespace AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;

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
    string? PropertyCustodianDesignation = null);

public sealed record GetOrganizationProfileQuery() : IQuery<OrganizationProfileDto?>;

public sealed record UpsertOrganizationProfileCommand(
    string Name,
    string? ShortName,
    string? Address,
    string? LogoUrl,
    string? AnnexECode = null,
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

