using FluentValidation;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;

namespace AMIS.Modules.MasterData.Features.v1.OrganizationProfile.UpsertOrganizationProfile;

public sealed class UpsertOrganizationProfileCommandValidator : AbstractValidator<UpsertOrganizationProfileCommand>
{
    public UpsertOrganizationProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortName).MaximumLength(80).When(x => x.ShortName is not null);
        RuleFor(x => x.Address).MaximumLength(400).When(x => x.Address is not null);
        RuleFor(x => x.LogoUrl).MaximumLength(500).When(x => x.LogoUrl is not null);
        RuleFor(x => x.RegionalManagerName).MaximumLength(200).When(x => x.RegionalManagerName is not null);
        RuleFor(x => x.RegionalManagerDesignation).MaximumLength(200).When(x => x.RegionalManagerDesignation is not null);
        RuleFor(x => x.AssistantRegionalManagerName).MaximumLength(200).When(x => x.AssistantRegionalManagerName is not null);
        RuleFor(x => x.AssistantRegionalManagerDesignation).MaximumLength(200).When(x => x.AssistantRegionalManagerDesignation is not null);
        RuleFor(x => x.AccountantName).MaximumLength(200).When(x => x.AccountantName is not null);
        RuleFor(x => x.AccountantDesignation).MaximumLength(200).When(x => x.AccountantDesignation is not null);
        RuleFor(x => x.SupervisingAdminOfficerName).MaximumLength(200).When(x => x.SupervisingAdminOfficerName is not null);
        RuleFor(x => x.SupervisingAdminOfficerDesignation).MaximumLength(200).When(x => x.SupervisingAdminOfficerDesignation is not null);
    }
}

