using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.RegisterAsset;

public sealed class RegisterAssetCommandValidator : AbstractValidator<RegisterAssetCommand>
{
    public RegisterAssetCommandValidator()
    {
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(400);
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(64);
        RuleFor(x => x.UnitCost).GreaterThan(0);
        RuleFor(x => x.SubMajorAccount).NotEmpty().Length(2)
            .WithMessage("SubMajorAccount must be exactly 2 characters (COA 2020-006).");
        RuleFor(x => x.GeneralLedgerAccount).NotEmpty().Length(2)
            .WithMessage("GeneralLedgerAccount must be exactly 2 characters (COA 2020-006).");
        RuleFor(x => x.LocationCode).NotEmpty().Length(2)
            .WithMessage("LocationCode must be exactly 2 characters (COA 2020-006).");
        RuleFor(x => x.SerialNo).MaximumLength(64);
        RuleFor(x => x.Brand).MaximumLength(64);
        RuleFor(x => x.Model).MaximumLength(64);

        // Form segregation guard at the command layer (rule #1):
        // SE/Low+High = SE; PPE category = PPE.
        RuleFor(x => x).Must(x =>
            (x.AssetType == AssetType.SE && (x.Category == AssetCategory.LowValuedSemi || x.Category == AssetCategory.HighValuedSemi)) ||
            (x.AssetType == AssetType.PPE && x.Category == AssetCategory.PPE))
            .WithMessage("AssetType and Category must align: SE → LowValuedSemi/HighValuedSemi, PPE → PPE.");
    }
}

