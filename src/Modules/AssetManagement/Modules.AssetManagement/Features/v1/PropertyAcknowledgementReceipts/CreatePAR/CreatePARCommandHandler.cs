using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.CreatePAR;

public sealed class CreatePARCommandHandler : ICommandHandler<CreatePARCommand, CreatePARResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreatePARCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CreatePARResult> Handle(CreatePARCommand command, CancellationToken cancellationToken)
    {
        var parNoInUse = await _dbContext.PropertyAcknowledgementReceipts
            .IgnoreQueryFilters()
            .AnyAsync(x => x.PARNo == command.PARNo, cancellationToken)
            .ConfigureAwait(false);

        if (parNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.PARNo), "A PAR with this number already exists.")
            ]);
        }

        var requestedPPEItemIds = command.Items.Select(x => x.PPEItemId).Distinct().ToList();
        var ppeItems = await _dbContext.PPEItems
            .Where(x => requestedPPEItemIds.Contains(x.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var ppeItemId in requestedPPEItemIds)
        {
            var ppeItem = ppeItems.FirstOrDefault(x => x.Id == ppeItemId)
                ?? throw new KeyNotFoundException($"PPE item with ID {ppeItemId} not found.");

            if (ppeItem.Status != PPEItemStatus.OnHand)
            {
                throw new FluentValidation.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(
                        nameof(ppeItemId),
                        $"PPE item '{ppeItem.PropertyCode}' is not available (Status: {ppeItem.Status}). Only OnHand items can be assigned via PAR.")
                ]);
            }
        }

        var par = PropertyAcknowledgementReceipt.Create(
            command.PARNo,
            command.Date,
            command.PARType,
            command.ReceivedFromEmployeeId,
            command.ReceivedByEmployeeId,
            command.ApprovedByEmployeeId);

        par.CreatedBy = _currentUser.GetUserId().ToString();
        _dbContext.PropertyAcknowledgementReceipts.Add(par);

        foreach (var (itemRequest, itemNo) in command.Items.Select((r, i) => (r, i + 1)))
        {
            var ppeItem = ppeItems.First(x => x.Id == itemRequest.PPEItemId);

            var parItem = PARItem.Create(
                par.Id,
                ppeItem.Id,
                itemNo,
                itemRequest.Quantity,
                itemRequest.Unit,
                itemRequest.ItemDescription,
                ppeItem.UnitCost,
                ppeItem.EstimatedUsefulLifeYears,
                ppeItem.DateAcquired);

            _dbContext.PARItems.Add(parItem);

            ppeItem.AssignPAR(command.ReceivedByEmployeeId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreatePARResult(par.Id, par.PARNo, command.Items.Count);
    }
}
