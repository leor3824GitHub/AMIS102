using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.ReturnAccountabilityLines;

public sealed class ReturnAccountabilityLinesCommandValidator : AbstractValidator<ReturnAccountabilityLinesCommand>
{
    public ReturnAccountabilityLinesCommandValidator()
    {
        RuleFor(x => x.AccountabilityId).NotEmpty();
        RuleFor(x => x.ReturnedOn).NotEqual(default(DateOnly));
        RuleFor(x => x.Lines).NotNull().NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.LineId).NotEmpty();
            line.RuleFor(l => l.OdometerAtReturn).GreaterThanOrEqualTo(0).When(l => l.OdometerAtReturn.HasValue);
        });
    }
}

