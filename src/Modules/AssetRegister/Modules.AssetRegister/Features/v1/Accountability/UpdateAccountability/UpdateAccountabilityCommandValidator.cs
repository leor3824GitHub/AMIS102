using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Data;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.UpdateAccountability;

public sealed class UpdateAccountabilityCommandValidator : AbstractValidator<UpdateAccountabilityCommand>
{
    public UpdateAccountabilityCommandValidator(AssetRegisterDbContext db)
    {
        RuleFor(x => x.AccountabilityId).NotEmpty();
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(64);
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

        // No duplicate assets on the same document.
        RuleFor(x => x.Lines).Must(lines => lines.Select(l => l.AssetRegistryId).Distinct().Count() == lines.Count)
            .WithMessage("The same asset cannot appear more than once on an accountability.");

        // ICS/PAR expiry policy is type-dependent and validated in the handler (type is immutable on the
        // loaded record). Here we only assert that a supplied expiry is after the issued date.
        RuleFor(x => x.ExpiresOn).GreaterThan(x => x.IssuedOn)
            .When(x => x.ExpiresOn is not null)
            .WithMessage("Expiry date must be after the issued date.");

        // COA Circular 2022-004 §4.14: low-valued and high-valued semi-expendable items must carry
        // separate ICS control numbers. The document number is immutable on update, so editing the
        // line set must not turn a single ICS into a low/high mix. A low+high mix is only ever
        // possible on an ICS (PPE assets carry Category.PPE), so no accountability-type gate is needed.
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
                ctx.AddFailure(nameof(UpdateAccountabilityCommand.Lines),
                    "An ICS cannot mix low-valued and high-valued semi-expendable items. " +
                    "Issue them on separate ICS documents (COA Circular 2022-004 §4.14).");
        });
    }
}
