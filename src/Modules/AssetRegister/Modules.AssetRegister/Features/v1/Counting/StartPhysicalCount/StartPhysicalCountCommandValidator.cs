using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.StartPhysicalCount;

public sealed class StartPhysicalCountCommandValidator : AbstractValidator<StartPhysicalCountCommand>
{
    public StartPhysicalCountCommandValidator()
    {
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(64);
        RuleFor(x => x.StartedOn).NotEqual(default(DateOnly));
        RuleFor(x => x.AsAt).NotEqual(default(DateOnly));
        RuleFor(x => x.ConductedBy).NotNull().NotEmpty();
        RuleForEach(x => x.ConductedBy).ChildRules(e =>
        {
            e.RuleFor(c => c.EmployeeId).NotEmpty();
            e.RuleFor(c => c.PrintedName).NotEmpty().MaximumLength(200);
        });
    }
}

