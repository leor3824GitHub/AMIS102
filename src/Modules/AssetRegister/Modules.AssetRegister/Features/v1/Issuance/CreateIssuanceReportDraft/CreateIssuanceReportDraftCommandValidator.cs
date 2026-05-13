using FluentValidation;
using FSH.Modules.AssetRegister.Contracts.v1.Issuance;

namespace FSH.Modules.AssetRegister.Features.v1.Issuance.CreateIssuanceReportDraft;

public sealed class CreateIssuanceReportDraftCommandValidator : AbstractValidator<CreateIssuanceReportDraftCommand>
{
    public CreateIssuanceReportDraftCommandValidator()
    {
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(64);
        RuleFor(x => x.PeriodFromDate).NotEqual(default(DateOnly));
        RuleFor(x => x.PeriodToDate).GreaterThanOrEqualTo(x => x.PeriodFromDate);
        RuleFor(x => x.PreparedBy).NotNull();
        RuleFor(x => x.PreparedBy.EmployeeId).NotEmpty().When(x => x.PreparedBy is not null);
        RuleFor(x => x.PreparedBy.PrintedName).NotEmpty().When(x => x.PreparedBy is not null);
    }
}
