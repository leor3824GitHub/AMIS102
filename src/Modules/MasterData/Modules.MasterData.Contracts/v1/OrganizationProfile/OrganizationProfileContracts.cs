using Mediator;

namespace FSH.Modules.MasterData.Contracts.v1.OrganizationProfile;

public sealed record OrganizationProfileDto(
    Guid Id,
    string Name,
    string? ShortName,
    string? Address,
    string? LogoUrl,
    string? AnnexECode);

public sealed record GetOrganizationProfileQuery() : IQuery<OrganizationProfileDto?>;

public sealed record UpsertOrganizationProfileCommand(
    string Name,
    string? ShortName,
    string? Address,
    string? LogoUrl,
    string? AnnexECode = null) : ICommand<OrganizationProfileDto>;
