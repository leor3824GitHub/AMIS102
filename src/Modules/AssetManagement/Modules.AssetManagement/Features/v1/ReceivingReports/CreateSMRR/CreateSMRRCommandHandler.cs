using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using FSH.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.CreateSMRR;

public sealed class CreateSMRRCommandHandler : ICommandHandler<CreateSMRRCommand, CreateSMRRResult>
{
    // Pseudo-codes used as the PropertyCodeCounter key for semi-expendable sequences.
    // These never collide with real COA GAM class/item codes (which are alphabetic).
    private const string SeCounterClassCode = "AM";
    private const string SeCounterItemCode  = "SE";

    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public CreateSMRRCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser, IMediator mediator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async ValueTask<CreateSMRRResult> Handle(CreateSMRRCommand command, CancellationToken cancellationToken)
    {
        var smrrNoInUse = await _dbContext.SuppliesMaterialsReceivingReports
            .IgnoreQueryFilters()
            .AnyAsync(x => x.SMRRNo == command.SMRRNo, cancellationToken)
            .ConfigureAwait(false);

        if (smrrNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.SMRRNo), "A receiving report with this SMRR number already exists.")
            ]);
        }

        var threshold = await _mediator.Send(new GetActiveCapitalizationThresholdQuery(), cancellationToken)
            .ConfigureAwait(false);

        if (threshold is null)
        {
            throw new NotFoundException("No active capitalization threshold is configured. Set one in Master Data → Capitalization Thresholds before receiving items.");
        }

        var requestedItemIds = command.Items.Select(x => x.ItemId).Distinct().ToList();
        var foundItemIds = await _dbContext.PropertyItemCatalog
            .Where(x => requestedItemIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToHashSetAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var itemId in requestedItemIds)
        {
            if (!foundItemIds.Contains(itemId))
                throw new KeyNotFoundException($"Item catalog entry with ID {itemId} not found.");
        }

        foreach (var itemRequest in command.Items)
        {
            if (itemRequest.UnitCost >= threshold.CapitalizationAmount)
            {
                throw new FluentValidation.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(
                        nameof(itemRequest.UnitCost),
                        $"Unit cost ₱{itemRequest.UnitCost:N2} meets or exceeds the capitalization threshold of ₱{threshold.CapitalizationAmount:N2}. Items at or above this amount must be recorded as Fixed Assets (PPE), not as semi-expendable property.")
                ]);
            }
        }

        var smrr = SuppliesMaterialsReceivingReport.Create(
            command.SMRRNo,
            command.Date,
            command.ReceivedFrom,
            command.Address,
            command.ReceiptType,
            command.OtherReceiptType,
            command.FundCluster,
            command.ReceivedByEmployeeId,
            command.NotedByEmployeeId);

        string userId = _currentUser.GetUserId().ToString();
        string tenantId = _currentUser.GetTenant() ?? string.Empty;
        smrr.CreatedBy = userId;
        _dbContext.SuppliesMaterialsReceivingReports.Add(smrr);

        int year = command.Date.Year;

        // Build SMRRItems upfront — they stay tracked across retries.
        var smrrItems = command.Items.Select(itemRequest =>
        {
            var smrrItem = SMRRItem.Create(
                smrr.Id,
                itemRequest.Reference,
                itemRequest.ItemId,
                itemRequest.Description,
                itemRequest.AcquisitionDate,
                itemRequest.Quantity,
                itemRequest.UnitCost);
            _dbContext.SMRRItems.Add(smrrItem);
            return (smrrItem, itemRequest);
        }).ToList();

        // Retry loop: handles optimistic concurrency conflicts on PropertyCodeCounter (xmin).
        // On conflict: detach stale counter and properties, reload counter fresh, regenerate codes.
        const int maxAttempts = 3;
        int propertiesCreated = 0;

        for (int attempt = 0; ; attempt++)
        {
            propertiesCreated = 0;

            // Detach properties and counter from any previous failed attempt
            foreach (var entry in _dbContext.ChangeTracker.Entries<SemiExpendableProperty>().ToList())
                entry.State = EntityState.Detached;
            foreach (var entry in _dbContext.ChangeTracker.Entries<PropertyCodeCounter>().ToList())
                entry.State = EntityState.Detached;

            // Load (or create) the single counter for all SE sequences in this year
            var counter = await _dbContext.PropertyCodeCounters
                .FirstOrDefaultAsync(c =>
                    c.TenantId  == tenantId &&
                    c.ClassCode == SeCounterClassCode &&
                    c.ItemCode  == SeCounterItemCode  &&
                    c.Year      == year,
                    cancellationToken)
                .ConfigureAwait(false);

            if (counter is null)
            {
                counter = PropertyCodeCounter.Start(tenantId, SeCounterClassCode, SeCounterItemCode, year);
                _dbContext.PropertyCodeCounters.Add(counter);
            }

            foreach (var (smrrItem, itemRequest) in smrrItems)
            {
                var category = itemRequest.UnitCost <= threshold.SemiExpendableLowValueThreshold
                    ? AssetCategory.LowValuedSemi
                    : AssetCategory.HighValuedSemi;

                for (int i = 0; i < itemRequest.Quantity; i++)
                {
                    var seq = counter.NextSequence();
                    var propertyNo = $"{year}-NFA-{tenantId}-{seq:D5}";

                    var property = SemiExpendableProperty.Create(
                        propertyNo,
                        itemRequest.ItemId,
                        category,
                        serialNo: null,
                        itemRequest.AcquisitionDate,
                        itemRequest.UnitCost,
                        command.FundCluster,
                        remarks: null,
                        smrrItemId: smrrItem.Id);

                    property.CreatedBy = userId;
                    _dbContext.SemiExpendableProperties.Add(property);
                    propertiesCreated++;
                }
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts - 1)
            {
                // Another request modified the counter concurrently.
                // The outer loop will detach and reload everything for the next attempt.
            }
        }

        return new CreateSMRRResult(smrr.Id, smrr.SMRRNo, propertiesCreated);
    }
}
