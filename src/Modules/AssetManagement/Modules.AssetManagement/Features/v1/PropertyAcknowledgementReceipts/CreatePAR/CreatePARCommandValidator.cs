using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.CreatePAR;

public sealed class CreatePARCommandValidator : AbstractValidator<CreatePARCommand>
{
    public CreatePARCommandValidator()
    {
        RuleFor(x => x.PARNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.PARType).IsInEnum();
        RuleFor(x => x.ReceivedFromEmployeeId).NotEmpty();
        RuleFor(x => x.ReceivedByEmployeeId).NotEmpty();
        RuleFor(x => x.ApprovedByEmployeeId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Items).SetValidator(new CreatePARItemRequestValidator());
    }
}

internal sealed class CreatePARItemRequestValidator : AbstractValidator<CreatePARItemRequest>
{
    public CreatePARItemRequestValidator()
    {
        RuleFor(x => x.TangibleInventoryItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ItemDescription).NotEmpty().MaximumLength(500);
    }
}
