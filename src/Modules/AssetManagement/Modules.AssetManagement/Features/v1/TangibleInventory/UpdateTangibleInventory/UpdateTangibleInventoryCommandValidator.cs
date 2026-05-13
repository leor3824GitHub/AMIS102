using FluentValidation;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.UpdateTangibleInventory;

public sealed class UpdateTangibleInventoryCommandValidator : AbstractValidator<UpdateTangibleInventoryCommand>
{
    public UpdateTangibleInventoryCommandValidator()
    {
        RuleFor(x => x.ReportNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ReceivedFrom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

