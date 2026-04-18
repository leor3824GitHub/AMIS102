using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.CreatePPEIR;

public sealed class CreatePPEIRCommandHandler : ICommandHandler<CreatePPEIRCommand, CreatePPEIRResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreatePPEIRCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CreatePPEIRResult> Handle(CreatePPEIRCommand command, CancellationToken cancellationToken)
    {
        var ppeirNoInUse = await _dbContext.PPEIssuanceReports
            .IgnoreQueryFilters()
            .AnyAsync(x => x.PPEIRNo == command.PPEIRNo, cancellationToken)
            .ConfigureAwait(false);

        if (ppeirNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.PPEIRNo), "A PPEIR with this number already exists.")
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

            if (ppeItem.Status is not (PPEItemStatus.OnHand or PPEItemStatus.IssuedPAR))
            {
                throw new FluentValidation.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(
                        nameof(ppeItemId),
                        $"PPE item '{ppeItem.PropertyCode}' cannot be transferred (Status: {ppeItem.Status}).")
                ]);
            }
        }

        string tenantId = _currentUser.GetTenant() ?? string.Empty;

        var ppeir = PPEIssuanceReport.Create(
            tenantId,
            command.PPEIRNo,
            command.Date,
            command.IssuedToEmployeeId,
            command.IssuedToOfficeAddress,
            command.IssuanceType,
            command.IssuedByEmployeeId,
            command.ReceivedByEmployeeId,
            command.ApprovedByEmployeeId,
            command.DateReceived,
            command.DriverName,
            command.BillOfLadingNo);

        ppeir.CreatedBy = _currentUser.GetUserId().ToString();
        _dbContext.PPEIssuanceReports.Add(ppeir);

        foreach (var (itemRequest, itemNo) in command.Items.Select((r, i) => (r, i + 1)))
        {
            var ppeItem = ppeItems.First(x => x.Id == itemRequest.PPEItemId);

            var ppeirItem = PPEIRItem.Create(
                ppeir.Id,
                ppeItem.Id,
                itemNo,
                ppeItem.PropertyCode,
                ppeItem.SerialNumber,
                ppeItem.Description,
                ppeItem.DateAcquired,
                ppeItem.UnitCost);

            _dbContext.PPEIRItems.Add(ppeirItem);
            ppeItem.MarkTransferred();
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreatePPEIRResult(ppeir.Id, ppeir.PPEIRNo, command.Items.Count);
    }
}
