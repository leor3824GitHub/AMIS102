using Mediator;

namespace AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;

public sealed record OrganizationProfileDto(
    Guid Id,
    string Name,
    string? ShortName,
    string? Address,
    string? LogoUrl,
    string? AnnexECode,
    Guid? RegionalManagerId = null,
    string? RegionalManagerName = null,
    string? RegionalManagerDesignation = null,
    Guid? AssistantRegionalManagerId = null,
    string? AssistantRegionalManagerName = null,
    string? AssistantRegionalManagerDesignation = null,
    Guid? AccountantId = null,
    string? AccountantName = null,
    string? AccountantDesignation = null,
    Guid? SupervisingAdminOfficerId = null,
    string? SupervisingAdminOfficerName = null,
    string? SupervisingAdminOfficerDesignation = null);

public sealed record GetOrganizationProfileQuery() : IQuery<OrganizationProfileDto?>;

public sealed record UpsertOrganizationProfileCommand(
    string Name,
    string? ShortName,
    string? Address,
    string? LogoUrl,
    string? AnnexECode = null,
    Guid? RegionalManagerId = null,
    string? RegionalManagerName = null,
    string? RegionalManagerDesignation = null,
    Guid? AssistantRegionalManagerId = null,
    string? AssistantRegionalManagerName = null,
    string? AssistantRegionalManagerDesignation = null,
    Guid? AccountantId = null,
    string? AccountantName = null,
    string? AccountantDesignation = null,
    Guid? SupervisingAdminOfficerId = null,
    string? SupervisingAdminOfficerName = null,
    string? SupervisingAdminOfficerDesignation = null) : ICommand<OrganizationProfileDto>;

