using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.DeletePropertyItemCatalog;

public sealed class DeletePropertyItemCatalogCommandValidator : AbstractValidator<DeletePropertyItemCatalogCommand>
{
    public DeletePropertyItemCatalogCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Catalog item ID is required.");
    }
}

