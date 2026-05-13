using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using AMIS.Modules.ProcurementPlanning.Domain.Ppmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.CreatePpmp;

public sealed class CreatePpmpCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreatePpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(CreatePpmpCommand command, CancellationToken cancellationToken)
    {
        var number = await GeneratePpmpNumberAsync(command.FiscalYear, command.OfficeCode, cancellationToken);

        var ppmp = Ppmp.Create(
            number,
            command.FiscalYear,
            command.Phase,
            command.OfficeCode,
            command.EndUserUnit,
            command.PreparedById,
            command.Items.Select(PpmpMapper.ToItemData));

        ppmp.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.Ppmps.Add(ppmp);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return PpmpMapper.ToDto(ppmp);
    }

    private async Task<string> GeneratePpmpNumberAsync(int fiscalYear, string officeCode, CancellationToken ct)
    {
        var prefix = $"PPMP-{fiscalYear}-{officeCode.ToUpperInvariant()}-";
        var lastNumber = await dbContext.Ppmps
            .IgnoreQueryFilters()
            .Where(x => x.PpmpNumber.StartsWith(prefix))
            .Select(x => x.PpmpNumber)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (lastNumber is not null && int.TryParse(lastNumber[prefix.Length..], out var last))
            next = last + 1;

        return $"{prefix}{next:000}";
    }
}

