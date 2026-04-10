using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.CreateICS;

public sealed class CreateICSCommandValidator : AbstractValidator<CreateICSCommand>
{
    public CreateICSCommandValidator()
    {
        RuleFor(x => x.ICSNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.FundCluster).MaximumLength(50);
        RuleFor(x => x.ReceivedByEmployeeId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Items).SetValidator(new CreateICSItemRequestValidator());
    }
}

internal sealed class CreateICSItemRequestValidator : AbstractValidator<CreateICSItemRequest>
{
    public CreateICSItemRequestValidator()
    {
        RuleFor(x => x.SemiExpendablePropertyId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
