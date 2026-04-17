using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using FSH.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.CreatePPERR;

public sealed class CreatePPERRCommandHandler : ICommandHandler<CreatePPERRCommand, CreatePPERRResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public CreatePPERRCommandHandler(
        AssetManagementDbContext dbContext,
        ICurrentUser currentUser,
        IMediator mediator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mediator = mediator;
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

        var orgProfile = await _mediator.Send(new GetOrganizationProfileQuery(), cancellationToken)
            .ConfigureAwait(false);
        var officeCode = orgProfile?.AnnexECode ?? "NFA";

        var pperr = PPEReceivingReport.Create(
            command.PPERRNo,
            command.Date,
            command.ReceivedFrom,
            command.Address,
            command.ReceiptNature,
            command.ReceivedByEmployeeId,
            command.NotedByEmployeeId);

        string userId = _currentUser.GetUserId().ToString();
        pperr.CreatedBy = userId;
        _dbContext.PPEReceivingReports.Add(pperr);

        int year = command.Date.Year;
        string tenantId = _currentUser.GetTenant() ?? string.Empty;

        // Build PPERRItems upfront (no property codes yet for auto-classified items).
        // These stay tracked across retries — their PropertyCode will be patched each attempt.
        var itemPairs = new List<(PPERRItem PperrItem, CreatePPERRItemRequest Request, bool HasClassification, string? ClassCode, string? ItemCode)>();

        foreach (var (itemRequest, itemNo) in command.Items.Select((r, i) => (r, i + 1)))
        {
            bool hasClassification = !string.IsNullOrWhiteSpace(itemRequest.ClassCode)
                                  && !string.IsNullOrWhiteSpace(itemRequest.ItemCode);

            string? resolvedClassCode = hasClassification ? itemRequest.ClassCode!.ToUpperInvariant() : null;
            string? resolvedItemCode  = hasClassification ? itemRequest.ItemCode!.ToUpperInvariant()  : null;

            var pperrItem = PPERRItem.Create(
                pperr.Id,
                itemNo,
                itemRequest.PropertyCode ?? string.Empty,
                itemRequest.Description,
                itemRequest.DateAcquired,
                itemRequest.Quantity,
                itemRequest.UnitCost,
                resolvedClassCode,
                resolvedItemCode,
                hasClassification ? officeCode : null);

            _dbContext.PPERRItems.Add(pperrItem);
            itemPairs.Add((pperrItem, itemRequest, hasClassification, resolvedClassCode, resolvedItemCode));
        }

        // Retry loop: handles optimistic concurrency conflicts on PropertyCodeCounter (xmin).
        // On conflict: detach stale counters and PPEItems, reload counters fresh, regenerate codes.
        const int maxAttempts = 3;
        int ppeItemsCreated = 0;

        for (int attempt = 0; ; attempt++)
        {
            ppeItemsCreated = 0;

            // Detach PPEItems and counters from any previous failed attempt
            foreach (var entry in _dbContext.ChangeTracker.Entries<PPEItem>().ToList())
                entry.State = EntityState.Detached;
            foreach (var entry in _dbContext.ChangeTracker.Entries<PropertyCodeCounter>().ToList())
                entry.State = EntityState.Detached;

            // Load (or create) counters fresh for each (ClassCode, ItemCode) pair in this command
            var counterKeys = itemPairs
                .Where(p => p.HasClassification)
                .Select(p => (ClassCode: p.ClassCode!, ItemCode: p.ItemCode!))
                .Distinct()
                .ToList();

            var counters = new Dictionary<(string ClassCode, string ItemCode), PropertyCodeCounter>();

            foreach (var key in counterKeys)
            {
                var counter = await _dbContext.PropertyCodeCounters
                    .FirstOrDefaultAsync(c =>
                        c.TenantId == tenantId &&
                        c.ClassCode == key.ClassCode &&
                        c.ItemCode  == key.ItemCode  &&
                        c.Year      == year,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (counter is null)
                {
                    counter = PropertyCodeCounter.Start(tenantId, key.ClassCode, key.ItemCode, year);
                    _dbContext.PropertyCodeCounters.Add(counter);
                }

                counters[key] = counter;
            }

            // Generate PPEItems; patch first auto-generated code back onto the PPERR line item
            foreach (var (pperrItem, itemRequest, hasClassification, resolvedClassCode, resolvedItemCode) in itemPairs)
            {
                string? firstPropertyCode = null;

                for (int i = 0; i < itemRequest.Quantity; i++)
                {
                    string propertyCode;

                    if (hasClassification)
                    {
                        var seq = counters[(resolvedClassCode!, resolvedItemCode!)].NextSequence();
                        propertyCode = $"{year}-NFA-{officeCode}-{resolvedClassCode}-{resolvedItemCode}-{seq:D3}";
                    }
                    else
                    {
                        propertyCode = itemRequest.PropertyCode ?? $"PPE-{year}-{ppeItemsCreated + 1:D5}";
                    }

                    firstPropertyCode ??= propertyCode;

                    var ppeItem = PPEItem.Create(
                        propertyCode,
                        propertyCode,
                        itemRequest.Description,
                        itemRequest.SerialNumber,
                        itemRequest.DateAcquired,
                        itemRequest.UnitCost,
                        itemRequest.EstimatedUsefulLifeYears,
                        pperr.Id,
                        resolvedClassCode,
                        resolvedItemCode,
                        hasClassification ? officeCode : null);

                    ppeItem.CreatedBy = userId;
                    _dbContext.PPEItems.Add(ppeItem);
                    ppeItemsCreated++;
                }

                // Stamp first generated code onto the PPERR line item for reporting
                if (hasClassification && firstPropertyCode is not null)
                    pperrItem.PatchPropertyCode(firstPropertyCode);
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts - 1)
            {
                // Another request modified a counter row concurrently.
                // The outer loop will detach and reload everything for the next attempt.
            }
        }

        return new CreatePPERRResult(pperr.Id, pperr.PPERRNo, ppeItemsCreated);
    }
}
