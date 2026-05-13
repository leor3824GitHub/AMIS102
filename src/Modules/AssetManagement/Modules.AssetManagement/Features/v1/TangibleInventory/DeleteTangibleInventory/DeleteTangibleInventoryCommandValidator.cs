using FluentValidation;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.DeleteTangibleInventory;

public sealed class DeleteTangibleInventoryCommandValidator : AbstractValidator<DeleteTangibleInventoryCommand>
{
    public DeleteTangibleInventoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

