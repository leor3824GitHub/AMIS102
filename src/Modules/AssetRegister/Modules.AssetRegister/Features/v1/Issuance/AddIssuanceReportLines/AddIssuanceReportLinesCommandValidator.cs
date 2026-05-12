using FluentValidation;
using FSH.Modules.AssetRegister.Contracts.v1.Issuance;

namespace FSH.Modules.AssetRegister.Features.v1.Issuance.AddIssuanceReportLines;

public sealed class AddIssuanceReportLinesCommandValidator : AbstractValidator<AddIssuanceReportLinesCommand>
{
    public AddIssuanceReportLinesCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.AccountabilityLineIds).NotNull().NotEmpty();
        RuleForEach(x => x.AccountabilityLineIds).NotEmpty();
    }
}
