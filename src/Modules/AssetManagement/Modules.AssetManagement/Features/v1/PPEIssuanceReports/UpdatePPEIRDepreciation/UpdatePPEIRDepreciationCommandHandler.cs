using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.PPEIssuanceReports.UpdatePPEIRDepreciation;

public sealed class UpdatePPEIRDepreciationCommandHandler : ICommandHandler<UpdatePPEIRDepreciationCommand, UpdatePPEIRDepreciationResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdatePPEIRDepreciationCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<UpdatePPEIRDepreciationResult> Handle(UpdatePPEIRDepreciationCommand command, CancellationToken cancellationToken)
    {
        var ppeirExists = await _dbContext.PPEIssuanceReports
            .AnyAsync(x => x.Id == command.PPEIRId, cancellationToken)
            .ConfigureAwait(false);

        if (!ppeirExists)
        {
            throw new KeyNotFoundException($"PPE Issuance Report with ID {command.PPEIRId} not found.");
        }

        var requestedItemIds = command.Items.Select(x => x.ItemId).Distinct().ToList();

        var ppeirItems = await _dbContext.PPEIRItems
            .Where(x => x.PPEIRId == command.PPEIRId && requestedItemIds.Contains(x.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var itemId in requestedItemIds)
        {
            if (!ppeirItems.Any(x => x.Id == itemId))
            {
                throw new KeyNotFoundException($"PPEIR item with ID {itemId} not found on report {command.PPEIRId}.");
            }
        }

        foreach (var request in command.Items)
        {
            var item = ppeirItems.First(x => x.Id == request.ItemId);
            item.SetDepreciation(request.AccumulatedDepreciation, request.BookValue);
        }

        var ppeir = await _dbContext.PPEIssuanceReports
            .FirstAsync(x => x.Id == command.PPEIRId, cancellationToken)
            .ConfigureAwait(false);

        ppeir.LastModifiedBy = _currentUser.GetUserId().ToString();
        ppeir.LastModifiedOnUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new UpdatePPEIRDepreciationResult(command.PPEIRId, ppeirItems.Count);
    }
}

