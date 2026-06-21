using AMIS.Framework.Core.Context;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;
using AMIS.Modules.BudgetDisbursement.Data;
using AMIS.Modules.BudgetDisbursement.Domain.Settings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.Settings.GetBudgetDisbursementSettings;

public sealed class GetBudgetDisbursementSettingsQueryHandler(BudgetDisbursementDbContext dbContext, ICurrentUser currentUser)
    : IQueryHandler<GetBudgetDisbursementSettingsQuery, BudgetDisbursementSettingsDto>
{
    public async ValueTask<BudgetDisbursementSettingsDto> Handle(GetBudgetDisbursementSettingsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.GetTenant() ?? string.Empty;

        var settings = await dbContext.BudgetDisbursementSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);

        // Return defaults when no row exists yet — admin hasn't changed anything.
        var effective = settings ?? BudgetDisbursementModuleSettings.CreateDefault(tenantId);
        return new BudgetDisbursementSettingsDto(
            effective.WatermarkSignedCopies,
            effective.DvSectionAName,
            effective.DvSectionADesignation,
            effective.DvSectionBName,
            effective.DvSectionBDesignation,
            effective.DvSectionCName,
            effective.DvSectionCDesignation,
            effective.BurSectionAName,
            effective.BurSectionADesignation,
            effective.BurSectionBName,
            effective.BurSectionBDesignation);
    }
}
