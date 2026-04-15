using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.CreatePPERR;

public sealed class CreatePPERRCommandHandler : ICommandHandler<CreatePPERRCommand, CreatePPERRResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreatePPERRCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CreatePPERRResult> Handle(CreatePPERRCommand command, CancellationToken cancellationToken)
    {
        var pperrNoInUse = await _dbContext.PPEReceivingReports
            .IgnoreQueryFilters()
            .AnyAsync(x => x.PPERRNo == command.PPERRNo, cancellationToken)
            .ConfigureAwait(false);

        if (pperrNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.PPERRNo), "A PPE receiving report with this PPERR number already exists.")
            ]);
        }

        var pperr = PPEReceivingReport.Create(
            command.PPERRNo,
            command.Date,
            command.ReceivedFrom,
            command.Address,
            command.ReceiptNature,
            command.ReceivedByEmployeeId,
            command.NotedByEmployeeId);

        pperr.CreatedBy = _currentUser.GetUserId().ToString();
        _dbContext.PPEReceivingReports.Add(pperr);

        int ppeItemsCreated = 0;
        int year = command.Date.Year;
        int sequence = await GetNextPPESequenceAsync(year, cancellationToken).ConfigureAwait(false);

        foreach (var (itemRequest, itemNo) in command.Items.Select((r, i) => (r, i + 1)))
        {
            var pperrItem = PPERRItem.Create(
                pperr.Id,
                itemNo,
                itemRequest.PropertyCode,
                itemRequest.Description,
                itemRequest.DateAcquired,
                itemRequest.Quantity,
                itemRequest.UnitCost);

            _dbContext.PPERRItems.Add(pperrItem);

            for (int i = 0; i < itemRequest.Quantity; i++)
            {
                var propertyNumber = $"PPE-{year}-{sequence:D5}";
                sequence++;

                var ppeItem = PPEItem.Create(
                    itemRequest.PropertyCode,
                    propertyNumber,
                    itemRequest.Description,
                    itemRequest.SerialNumber,
                    itemRequest.DateAcquired,
                    itemRequest.UnitCost,
                    itemRequest.EstimatedUsefulLifeYears,
                    pperr.Id);

                ppeItem.CreatedBy = _currentUser.GetUserId().ToString();
                _dbContext.PPEItems.Add(ppeItem);
                ppeItemsCreated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreatePPERRResult(pperr.Id, pperr.PPERRNo, ppeItemsCreated);
    }

    private async Task<int> GetNextPPESequenceAsync(int year, CancellationToken cancellationToken)
    {
        var prefix = $"PPE-{year}-";
        var lastNo = await _dbContext.PPEItems
            .IgnoreQueryFilters()
            .Where(x => x.PropertyNumber.StartsWith(prefix))
            .OrderByDescending(x => x.PropertyNumber)
            .Select(x => x.PropertyNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (lastNo is null) return 1;

        var sequencePart = lastNo[prefix.Length..];
        return int.TryParse(sequencePart, out var seq) ? seq + 1 : 1;
    }
}
