using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.DeleteLocation;

public sealed class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
{
    public DeleteLocationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
