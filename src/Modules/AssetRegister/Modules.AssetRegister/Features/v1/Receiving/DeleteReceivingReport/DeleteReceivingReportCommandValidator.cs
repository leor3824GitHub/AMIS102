using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.DeleteReceivingReport;

public sealed class DeleteReceivingReportCommandValidator : AbstractValidator<DeleteReceivingReportCommand>
{
    public DeleteReceivingReportCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
