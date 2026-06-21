using AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;
using FluentValidation;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.Settings.UpdateBudgetDisbursementSettings;

public sealed class UpdateBudgetDisbursementSettingsCommandValidator : AbstractValidator<UpdateBudgetDisbursementSettingsCommand>
{
    public UpdateBudgetDisbursementSettingsCommandValidator()
    {
        // WatermarkSignedCopies is a non-nullable bool — always valid.
    }
}
