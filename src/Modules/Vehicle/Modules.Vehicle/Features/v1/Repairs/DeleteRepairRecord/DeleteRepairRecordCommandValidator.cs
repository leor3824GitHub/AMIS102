using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.DeleteRepairRecord;

public sealed class DeleteRepairRecordCommandValidator : AbstractValidator<DeleteRepairRecordCommand>
{
    public DeleteRepairRecordCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
