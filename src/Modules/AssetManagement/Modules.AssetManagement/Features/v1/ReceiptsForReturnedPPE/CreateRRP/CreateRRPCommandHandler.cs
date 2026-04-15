using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.CreateRRP;

public sealed class CreateRRPCommandHandler : ICommandHandler<CreateRRPCommand, CreateRRPResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateRRPCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CreateRRPResult> Handle(CreateRRPCommand command, CancellationToken cancellationToken)
    {
        var rrpNoInUse = await _dbContext.ReceiptsForReturnedPPE
            .IgnoreQueryFilters()
            .AnyAsync(x => x.RRPNo == command.RRPNo, cancellationToken)
            .ConfigureAwait(false);

        if (rrpNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.RRPNo), "An RRP with this number already exists.")
            ]);
        }

        var requestedIds = command.Items.Select(x => x.PPEItemId).Distinct().ToList();
        var ppeItems = await _dbContext.PPEItems
            .Where(x => requestedIds.Contains(x.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var ppeItemId in requestedIds)
        {
            var ppeItem = ppeItems.FirstOrDefault(x => x.Id == ppeItemId)
                ?? throw new KeyNotFoundException($"PPE item with ID {ppeItemId} not found.");

            if (ppeItem.Status == PPEItemStatus.Disposed)
            {
                throw new FluentValidation.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(
                        nameof(ppeItemId),
                        $"PPE item '{ppeItem.PropertyCode}' is already disposed and cannot be returned.")
                ]);
            }
        }

        var rrp = ReceiptForReturnedPPE.Create(
            command.RRPNo,
            command.Date,
            command.ReturnCategory,
            command.ReturnedByEmployeeId,
            command.ApprovedByEmployeeId,
            command.SignedByEmployeeId,
            command.PropertyInspectorCertified);

        rrp.CreatedBy = _currentUser.GetUserId().ToString();
        _dbContext.ReceiptsForReturnedPPE.Add(rrp);

        foreach (var (itemRequest, itemNo) in command.Items.Select((r, i) => (r, i + 1)))
        {
            var ppeItem = ppeItems.First(x => x.Id == itemRequest.PPEItemId);

            var rrpItem = RRPItem.Create(
                rrp.Id,
                ppeItem.Id,
                itemNo,
                itemRequest.SourceDocumentRef,
                ppeItem.PropertyCode,
                ppeItem.Description,
                itemRequest.Quantity,
                ppeItem.UnitCost);

            _dbContext.RRPItems.Add(rrpItem);

            if (command.ReturnCategory == PPEReturnCategory.Serviceable)
            {
                ppeItem.ReturnToStock();
            }
            else
            {
                ppeItem.MarkDisposed();
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateRRPResult(rrp.Id, rrp.RRPNo, command.Items.Count);
    }
}
