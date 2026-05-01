using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ConsolidatePpmps;

public sealed class ConsolidatePpmpsCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<ConsolidatePpmpsCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        ConsolidatePpmpsCommand command, CancellationToken cancellationToken)
    {
        if (command.PpmpIds is null || command.PpmpIds.Count == 0)
            throw new CustomException("Select at least one PPMP to consolidate.", Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

        var selectedIds = command.PpmpIds.Distinct().ToList();
        var userId = currentUser.GetUserId();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var app = await dbContext.AnnualProcurementPlans
                .Include(x => x.SourcePpmps)
                .Include(x => x.LineItems)
                .FirstOrDefaultAsync(x => x.Id == command.AppId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new CustomException($"APP {command.AppId} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

            var existingSourcePpmpIds = app.SourcePpmps.Select(x => x.PpmpId).ToList();

            var ppmps = await dbContext.Ppmps
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => selectedIds.Contains(x.Id) &&
                            (x.Status == PpmpStatus.Approved ||
                             (x.Status == PpmpStatus.Consolidated && existingSourcePpmpIds.Contains(x.Id))))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var foundIds = ppmps.Select(x => x.Id).ToHashSet();
            var missingIds = selectedIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Count > 0)
                throw new CustomException(
                    "Some selected PPMPs are no longer eligible. Please refresh and try again.",
                    missingIds.Select(x => x.ToString()),
                    HttpStatusCode.Conflict);

            if (ppmps.Count == 0)
                throw new CustomException("No eligible PPMPs found for consolidation.", Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

            try
            {
                app.ConsolidatePpmps(ppmps, userId);
            }
            catch (InvalidOperationException ex)
            {
                throw new CustomException(ex.Message, Enumerable.Empty<string>(), HttpStatusCode.Conflict);
            }

            var approvedIds = ppmps.Where(x => x.Status == PpmpStatus.Approved).Select(x => x.Id).ToList();
            if (approvedIds.Count > 0)
            {
                var ppmpNow = DateTimeOffset.UtcNow;
                var updatedPpmps = await dbContext.Ppmps
                    .Where(x => approvedIds.Contains(x.Id) && x.Status == PpmpStatus.Approved)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, PpmpStatus.Consolidated)
                        .SetProperty(x => x.LastModifiedOnUtc, ppmpNow),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (updatedPpmps != approvedIds.Count)
                    throw new CustomException(
                        "Some selected PPMPs changed state while consolidating. Please refresh and try again.",
                        Enumerable.Empty<string>(),
                        HttpStatusCode.Conflict);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return await AppReadProjection.BuildDtoAsync(dbContext, command.AppId, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}