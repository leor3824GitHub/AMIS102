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
            item.RuleFor(i => i.CatalogItemId)
                .NotNull().NotEqual(Guid.Empty)
                .WithMessage("CatalogItemId is required on every receiving line. Pick a catalog item on the source PR/PPERR form.");
            item.RuleFor(i => i.UacsObjectCode).MaximumLength(64);
            item.RuleFor(i => i.SourceAgencyName).MaximumLength(200);
            item.RuleFor(i => i.SourcePropertyNo).MaximumLength(64);
            item.RuleFor(i => i.SourceDocumentRef).MaximumLength(200);

            item.RuleFor(i => i.AccumulatedDepreciation)
                .GreaterThanOrEqualTo(0m).When(i => i.AccumulatedDepreciation.HasValue)
                .WithMessage("Carried-over accumulated depreciation cannot be negative.");
            item.RuleFor(i => i.AccumulatedDepreciation)
                .LessThanOrEqualTo(i => i.UnitCost).When(i => i.AccumulatedDepreciation.HasValue)
                .WithMessage("Carried-over accumulated depreciation cannot exceed the line's unit cost.");

            // The carried amount and the period it covers must travel together. With the amount alone the
            // receiving agency's DepreciatedThrough stays null and the posting service replays the schedule
            // from the original acquisition date, double-charging the years the source agency already booked.
            item.RuleFor(i => i.DepreciationCurrentThrough)
                .NotNull()
                .When(i => i.AccumulatedDepreciation is > 0m)
                .WithMessage("DepreciationCurrentThrough is required when AccumulatedDepreciation is supplied — " +
                             "it is the last period the carried amount covers.");
            item.RuleFor(i => i.OriginalAcquisitionDate)
                .NotNull()
                .When(i => i.AccumulatedDepreciation is > 0m)
                .WithMessage("OriginalAcquisitionDate is required when carrying accumulated depreciation — " +
                             "the receiving agency continues the source agency's timeline.");

            // Either the line came from an internal Accepted IAR, OR it must declare its external source.
            item.RuleFor(i => i.SourceAgencyName)
                .NotEmpty()
                .When(i => i.SourceIARId is null || i.SourceIARId == Guid.Empty)
                .WithMessage("SourceAgencyName is required for ad-hoc receiving lines (no source IAR). " +
                             "Specify the originating office/agency (e.g. 'NFA Region V' or 'Department of Agriculture').");
        });
    }
}

