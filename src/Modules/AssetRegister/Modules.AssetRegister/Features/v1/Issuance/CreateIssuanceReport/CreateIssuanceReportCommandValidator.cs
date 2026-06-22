using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.CreateIssuanceReport;

public sealed class CreateIssuanceReportCommandValidator : AbstractValidator<CreateIssuanceReportCommand>
{
    public CreateIssuanceReportCommandValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(64);
        RuleFor(x => x.IssuedToOfficeAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Remarks).MaximumLength(1000).When(x => x.Remarks is not null);
        RuleFor(x => x.AssetRegistryIds).NotEmpty().WithMessage("At least one asset must be selected.");

        RuleFor(x => x.IssuedBy).NotNull();
        RuleFor(x => x.IssuedBy.EmployeeId).NotEmpty().When(x => x.IssuedBy is not null);
        RuleFor(x => x.IssuedBy.PrintedName).NotEmpty().MaximumLength(200).When(x => x.IssuedBy is not null);

        // ApprovedBy is resolved server-side from the Organization Profile (see handler).

        RuleFor(x => x.IssuedTo).NotNull();
        RuleFor(x => x.IssuedTo.PrintedName).NotEmpty().MaximumLength(200).When(x => x.IssuedTo is not null);
    }
}
