using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.IssueAccountability;

public sealed class IssueAccountabilityCommandValidator : AbstractValidator<IssueAccountabilityCommand>
{
    public IssueAccountabilityCommandValidator()
    {
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(64);
        RuleFor(x => x.IssuedBy).NotNull();
        RuleFor(x => x.IssuedBy.EmployeeId).NotEmpty().When(x => x.IssuedBy is not null);
        RuleFor(x => x.IssuedBy.PrintedName).NotEmpty().When(x => x.IssuedBy is not null);
        RuleFor(x => x.ReceivedBy).NotNull();
        RuleFor(x => x.ReceivedBy.EmployeeId).NotEmpty().When(x => x.ReceivedBy is not null);
        RuleFor(x => x.ReceivedBy.PrintedName).NotEmpty().When(x => x.ReceivedBy is not null);
        RuleFor(x => x.IssuedOn).NotEqual(default(DateOnly));
        RuleFor(x => x.Lines).NotNull().NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AssetRegistryId).NotEmpty();
            line.RuleFor(l => l.ItemNo).NotEmpty().MaximumLength(32);
            line.RuleFor(l => l.LocationId).NotEmpty();
            line.RuleFor(l => l.OdometerAtIssue).GreaterThanOrEqualTo(0).When(l => l.OdometerAtIssue.HasValue);
            line.RuleFor(l => l.PlateNumber).MaximumLength(32);
            line.RuleFor(l => l.EngineNumber).MaximumLength(64);
            line.RuleFor(l => l.ChassisNumber).MaximumLength(64);
        });

        // Form segregation: SE_ICS REQUIRES expiry; PPE_PAR FORBIDS it.
        RuleFor(x => x).Custom((c, ctx) =>
        {
            if (c.AccountabilityType == AccountabilityType.SE_ICS && c.ExpiresOn is null)
                ctx.AddFailure(nameof(c.ExpiresOn), "ICS (SE) accountability requires an ExpiresOn date.");
            if (c.AccountabilityType == AccountabilityType.PPE_PAR && c.ExpiresOn is not null)
                ctx.AddFailure(nameof(c.ExpiresOn), "PAR (PPE) accountability does not carry an ExpiresOn date.");
            if (c.ExpiresOn is { } eo && eo <= c.IssuedOn)
                ctx.AddFailure(nameof(c.ExpiresOn), "ExpiresOn must be after IssuedOn.");
        });
    }
}

