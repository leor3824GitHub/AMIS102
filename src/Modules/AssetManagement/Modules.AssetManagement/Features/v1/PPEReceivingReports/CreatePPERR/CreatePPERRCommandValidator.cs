using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.CreatePPERR;

public sealed class CreatePPERRCommandValidator : AbstractValidator<CreatePPERRCommand>
{
    public CreatePPERRCommandValidator()
    {
        RuleFor(x => x.PPERRNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.ReceivedFrom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.ReceiptNature).IsInEnum();
        RuleFor(x => x.ReceivedByEmployeeId).NotEmpty();
        RuleFor(x => x.NotedByEmployeeId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Items).SetValidator(new CreatePPERRItemRequestValidator());
    }
}

internal sealed class CreatePPERRItemRequestValidator : AbstractValidator<CreatePPERRItemRequest>
{
    public CreatePPERRItemRequestValidator()
    {
        // Either ClassCode+ItemCode (auto-generate) or explicit PropertyCode must be supplied
        RuleFor(x => x)
            .Must(x => (!string.IsNullOrWhiteSpace(x.ClassCode) && !string.IsNullOrWhiteSpace(x.ItemCode))
                    || !string.IsNullOrWhiteSpace(x.PropertyCode))
            .WithName("PropertyCode")
            .WithMessage("Provide either ClassCode + ItemCode (to auto-generate) or an explicit PropertyCode.");

        RuleFor(x => x.ClassCode).MaximumLength(4).When(x => x.ClassCode is not null);
        RuleFor(x => x.ItemCode).MaximumLength(2).When(x => x.ItemCode is not null);
        RuleFor(x => x.PropertyCode).MaximumLength(32).When(x => x.PropertyCode is not null);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.DateAcquired).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThan(0);
        RuleFor(x => x.EstimatedUsefulLifeYears).GreaterThan(0);
    }
}
