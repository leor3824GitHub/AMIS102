using FluentValidation;
using AMIS.Modules.MasterData.Contracts.v1.References;

namespace AMIS.Modules.MasterData.Features.v1.Offices.DeleteOffice;

public sealed class DeleteOfficeCommandValidator : AbstractValidator<DeleteOfficeCommand>
{
    public DeleteOfficeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Office id is required");
    }
}

