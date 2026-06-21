using AMIS.Framework.Core.Context;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;
using AMIS.Modules.BudgetDisbursement.Data;
using AMIS.Modules.BudgetDisbursement.Domain.Settings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.Settings.UpdateBudgetDisbursementSettings;

public sealed class UpdateBudgetDisbursementSettingsCommandHandler(BudgetDisbursementDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<UpdateBudgetDisbursementSettingsCommand>
{
    public async ValueTask<Unit> Handle(UpdateBudgetDisbursementSettingsCommand command, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.GetTenant() ?? string.Empty;

        var settings = await dbContext.BudgetDisbursementSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (settings is null)
        {
            settings = BudgetDisbursementModuleSettings.CreateDefault(tenantId);
            dbContext.BudgetDisbursementSettings.Add(settings);
        }

        settings.WatermarkSignedCopies = command.WatermarkSignedCopies;

        settings.DvSectionAName        = string.IsNullOrWhiteSpace(command.DvSectionAName)        ? null : command.DvSectionAName.Trim();
        settings.DvSectionADesignation = string.IsNullOrWhiteSpace(command.DvSectionADesignation) ? null : command.DvSectionADesignation.Trim();
        settings.DvSectionBName        = string.IsNullOrWhiteSpace(command.DvSectionBName)        ? null : command.DvSectionBName.Trim();
        settings.DvSectionBDesignation = string.IsNullOrWhiteSpace(command.DvSectionBDesignation) ? null : command.DvSectionBDesignation.Trim();
        settings.DvSectionCName        = string.IsNullOrWhiteSpace(command.DvSectionCName)        ? null : command.DvSectionCName.Trim();
        settings.DvSectionCDesignation = string.IsNullOrWhiteSpace(command.DvSectionCDesignation) ? null : command.DvSectionCDesignation.Trim();

        settings.BurSectionAName        = string.IsNullOrWhiteSpace(command.BurSectionAName)        ? null : command.BurSectionAName.Trim();
        settings.BurSectionADesignation = string.IsNullOrWhiteSpace(command.BurSectionADesignation) ? null : command.BurSectionADesignation.Trim();
        settings.BurSectionBName        = string.IsNullOrWhiteSpace(command.BurSectionBName)        ? null : command.BurSectionBName.Trim();
        settings.BurSectionBDesignation = string.IsNullOrWhiteSpace(command.BurSectionBDesignation) ? null : command.BurSectionBDesignation.Trim();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
