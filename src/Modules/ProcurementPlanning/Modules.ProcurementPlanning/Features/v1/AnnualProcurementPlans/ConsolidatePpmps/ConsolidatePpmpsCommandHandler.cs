using System.Net;
using System.Security.Cryptography;
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.LineReferences)
            .FirstOrDefaultAsync(x => x.Id == command.AppId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"APP {command.AppId} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        var ppmps = await dbContext.Ppmps
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => selectedIds.Contains(x.Id) &&
                        (x.Status == PpmpStatus.Approved ||
                         (x.Status == PpmpStatus.Consolidated && x.AppId == command.AppId)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var foundIds = ppmps.Select(x => x.Id).ToHashSet();
        var missingIds = selectedIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new CustomException(
                "Some selected PPMPs are no longer eligible. Please refresh and try again.",
                missingIds.Select(x => x.ToString()),
                HttpStatusCode.Conflict);
        }

        if (ppmps.Count == 0)
            throw new CustomException("No eligible PPMPs found for consolidation.", Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

<<<<<<< HEAD
        var userId = currentUser.GetUserId();
        app.ConsolidatePpmps(ppmps, userId);
=======
        var userId = currentUser.GetUserId().ToString();
        try
        {
            app.ConsolidatePpmps(ppmps, userId);
        }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, Enumerable.Empty<string>(), HttpStatusCode.Conflict);
        }
>>>>>>> d63aec54a5aea0527fd07e545543a98aceae4138

        var approvedIds = ppmps
            .Where(x => x.Status == PpmpStatus.Approved)
            .Select(x => x.Id)
            .ToList();

        if (approvedIds.Count > 0)
        {
            // Use set-based update to reduce per-row tracked updates and avoid unnecessary row-version contention.
            var now = DateTimeOffset.UtcNow;
            var updatedCount = await dbContext.Ppmps
                .Where(x => approvedIds.Contains(x.Id) && x.Status == PpmpStatus.Approved)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, PpmpStatus.Consolidated)
                    .SetProperty(x => x.AppId, app.Id)
                    .SetProperty(x => x.LastModifiedOnUtc, now)
                    .SetProperty(x => x.Version, RandomNumberGenerator.GetBytes(8)),
                    cancellationToken)
                .ConfigureAwait(false);

            if (updatedCount != approvedIds.Count)
            {
                throw new CustomException(
                    "Some selected PPMPs changed state while consolidating. Please refresh and try again.",
                    Enumerable.Empty<string>(),
                    HttpStatusCode.Conflict);
            }
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CustomException(
                "Consolidation failed because the APP was updated by another user. Please refresh and try again.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);
        }

        return await AppReadProjection.BuildDtoAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
    }
}
