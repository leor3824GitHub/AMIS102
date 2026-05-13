using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using AMIS.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateAnnualProcurementPlan;

public sealed class CreateAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateAnnualProcurementPlanCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        CreateAnnualProcurementPlanCommand command, CancellationToken cancellationToken)
    {
        var hasDuplicate = await dbContext.AnnualProcurementPlans
            .AnyAsync(x => x.FiscalYear == command.FiscalYear
                        && x.IsCurrentVersion
                        && x.Status != AppStatus.Superseded, cancellationToken)
            .ConfigureAwait(false);

        if (hasDuplicate)
            throw new CustomException(
                $"An APP already exists for fiscal year {command.FiscalYear}. Use 'Amend' to create a new version.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        var appNumber = await GenerateAppNumberAsync(command.FiscalYear, cancellationToken);

        var app = AnnualProcurementPlan.Create(appNumber, command.FiscalYear, command.Phase);
        app.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.AnnualProcurementPlans.Add(app);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await AppReadProjection.BuildDtoAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
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

