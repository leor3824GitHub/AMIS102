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
    }
}

