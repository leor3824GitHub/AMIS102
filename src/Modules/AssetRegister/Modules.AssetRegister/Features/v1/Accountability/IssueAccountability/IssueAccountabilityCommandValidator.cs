using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Data;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.IssueAccountability;

public sealed class IssueAccountabilityCommandValidator : AbstractValidator<IssueAccountabilityCommand>
{
    public IssueAccountabilityCommandValidator(AssetRegisterDbContext db)
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

        // ICS carries a user-supplied expiry; PAR expiry is derived server-side (issued + 3 years),
        // so only the ICS-supplied value needs validating here.
        RuleFor(x => x).Custom((c, ctx) =>
        {
            if (c.AccountabilityType != AccountabilityType.SE_ICS)
                return;

            if (c.ExpiresOn is null)
                ctx.AddFailure(nameof(c.ExpiresOn), "ICS accountability requires an expiry date.");
            else if (c.ExpiresOn <= c.IssuedOn)
                ctx.AddFailure(nameof(c.ExpiresOn), "Expiry date must be after the issued date.");
        });

        // COA Circular 2022-004 §4.14: low-valued and high-valued semi-expendable items must
        // carry separate ICS control numbers, so a single ICS may not mix the two categories.
        // A low+high mix is only ever possible on an ICS (PPE assets carry Category.PPE), so no
        // accountability-type gate is needed here.
        RuleFor(x => x.Lines).CustomAsync(async (lines, ctx, ct) =>
        {
            if (lines is null || lines.Count == 0)
                return;

            var assetIds = lines.Select(l => l.AssetRegistryId).Distinct().ToList();
            var categories = await db.AssetRegistries
                .Where(a => assetIds.Contains(a.Id))
                .Select(a => a.Category)
                .Distinct()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (categories.Contains(AssetCategory.LowValuedSemi) && categories.Contains(AssetCategory.HighValuedSemi))
                ctx.AddFailure(nameof(IssueAccountabilityCommand.Lines),
                    "An ICS cannot mix low-valued and high-valued semi-expendable items. " +
                    "Issue them on separate ICS documents (COA Circular 2022-004 §4.14).");
        });
    }
}

