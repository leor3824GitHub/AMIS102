using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateAnnualProcurementPlan;

public sealed class CreateAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateAnnualProcurementPlanCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        CreateAnnualProcurementPlanCommand command, CancellationToken cancellationToken)
    {
        var appNumber = await GenerateAppNumberAsync(command.FiscalYear, cancellationToken);

        var app = AnnualProcurementPlan.Create(appNumber, command.FiscalYear, command.Phase);
        app.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.AnnualProcurementPlans.Add(app);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return AppMapper.ToDto(app);
    }

    private async Task<string> GenerateAppNumberAsync(int fiscalYear, CancellationToken ct)
    {
        var prefix = $"APP-{fiscalYear}-";
        var lastNumber = await dbContext.AnnualProcurementPlans
            .IgnoreQueryFilters()
            .Where(x => x.AppNumber.StartsWith(prefix))
            .Select(x => x.AppNumber)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (lastNumber is not null && int.TryParse(lastNumber[prefix.Length..], out var last))
            next = last + 1;

        return $"{prefix}{next:000}";
    }
}
