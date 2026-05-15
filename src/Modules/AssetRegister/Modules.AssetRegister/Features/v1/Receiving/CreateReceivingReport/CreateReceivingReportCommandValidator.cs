using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.CreateReceivingReport;

public sealed class CreateReceivingReportCommandValidator : AbstractValidator<CreateReceivingReportCommand>
{
    public CreateReceivingReportCommandValidator()
    {
        RuleFor(x => x.ReceivedFrom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.FundCluster).MaximumLength(64);
        RuleFor(x => x.OtherReceiptType)
            .NotEmpty().When(x => x.ReceiptType == ReceiptType.Other)
            .WithMessage("OtherReceiptType is required when ReceiptType is Other.");
        RuleFor(x => x.ReceivedBy).NotNull();
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item line is required.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty().MaximumLength(500);
            item.RuleFor(i => i.UnitCost).GreaterThan(0m);
            item.RuleFor(i => i.PropertyNo).NotEmpty().MaximumLength(32)
                .WithMessage("PropertyNo must be 1–32 characters.");
        });
    }
}

