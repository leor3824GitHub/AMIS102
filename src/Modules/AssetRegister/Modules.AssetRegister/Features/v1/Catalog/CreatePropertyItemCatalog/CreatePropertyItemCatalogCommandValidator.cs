using FluentValidation;
using FSH.Modules.AssetRegister.Contracts.v1.Catalog;

namespace FSH.Modules.AssetRegister.Features.v1.Catalog.CreatePropertyItemCatalog;

public sealed class CreatePropertyItemCatalogCommandValidator : AbstractValidator<CreatePropertyItemCatalogCommand>
{
    public CreatePropertyItemCatalogCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(400);
        RuleFor(x => x.DefaultPropertyClass).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DefaultCategoryCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DefaultUnit).NotEmpty().MaximumLength(32);
        RuleFor(x => x.UacsObjectCode).MaximumLength(32);
        RuleFor(x => x.EstimatedUsefulLifeYears).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
