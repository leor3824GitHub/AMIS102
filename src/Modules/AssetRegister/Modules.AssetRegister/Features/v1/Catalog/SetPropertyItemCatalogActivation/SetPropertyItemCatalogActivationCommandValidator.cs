using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.SetPropertyItemCatalogActivation;

public sealed class SetPropertyItemCatalogActivationCommandValidator : AbstractValidator<SetPropertyItemCatalogActivationCommand>
{
    public SetPropertyItemCatalogActivationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Catalog item ID is required.");
    }
}

