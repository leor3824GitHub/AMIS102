using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.CreatePPERR;

public sealed class CreatePPERRCommandValidator : AbstractValidator<CreatePPERRCommand>
{
    public CreatePPERRCommandValidator()
    {
        RuleFor(x => x.PPERRNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.ReceivedFrom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
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
        RuleFor(x => x.PropertyCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.DateAcquired).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThan(0);
        RuleFor(x => x.EstimatedUsefulLifeYears).GreaterThan(0);
    }
}
