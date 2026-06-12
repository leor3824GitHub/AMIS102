using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.UpdateIssuanceReportDepreciation;

public sealed class UpdateIssuanceReportDepreciationCommandValidator
    : AbstractValidator<UpdateIssuanceReportDepreciationCommand>
{
    public UpdateIssuanceReportDepreciationCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line depreciation entry is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.LineId).NotEmpty();
            line.RuleFor(l => l.AccumulatedDepreciation).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.BookValue).GreaterThanOrEqualTo(0);
        });
    }
}
