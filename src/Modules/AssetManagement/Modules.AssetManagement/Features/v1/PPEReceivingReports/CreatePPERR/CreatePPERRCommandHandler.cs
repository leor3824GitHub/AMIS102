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

        // Resolve tenant office code once for all items in this PPERR
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

        pperr.CreatedBy = _currentUser.GetUserId().ToString();
        _dbContext.PPEReceivingReports.Add(pperr);

        int ppeItemsCreated = 0;
        int year = command.Date.Year;

        // Pre-load or create counters for all unique (ClassCode, ItemCode) combinations in this command
        var counterKeys = command.Items
            .Where(x => !string.IsNullOrWhiteSpace(x.ClassCode) && !string.IsNullOrWhiteSpace(x.ItemCode))
            .Select(x => (ClassCode: x.ClassCode!.ToUpperInvariant(), ItemCode: x.ItemCode!.ToUpperInvariant()))
            .Distinct()
            .ToList();

        var counters = new Dictionary<(string ClassCode, string ItemCode), PropertyCodeCounter>();
        var tenantId = _currentUser.GetTenant() ?? string.Empty;

        foreach (var key in counterKeys)
        {
            var counter = await _dbContext.PropertyCodeCounters
                .FirstOrDefaultAsync(c =>
                    c.TenantId == tenantId &&
                    c.ClassCode == key.ClassCode &&
                    c.ItemCode == key.ItemCode &&
                    c.Year == year,
                    cancellationToken)
                .ConfigureAwait(false);

            if (counter is null)
            {
                counter = PropertyCodeCounter.Start(tenantId, key.ClassCode, key.ItemCode, year);
                _dbContext.PropertyCodeCounters.Add(counter);
            }

            counters[key] = counter;
        }

        foreach (var (itemRequest, itemNo) in command.Items.Select((r, i) => (r, i + 1)))
        {
            bool hasClassification = !string.IsNullOrWhiteSpace(itemRequest.ClassCode)
                                  && !string.IsNullOrWhiteSpace(itemRequest.ItemCode);

            string? resolvedClassCode = hasClassification ? itemRequest.ClassCode!.ToUpperInvariant() : null;
            string? resolvedItemCode = hasClassification ? itemRequest.ItemCode!.ToUpperInvariant() : null;

            var pperrItem = PPERRItem.Create(
                pperr.Id,
                itemNo,
                itemRequest.PropertyCode ?? string.Empty,   // will be patched per-unit below if auto-generated
                itemRequest.Description,
                itemRequest.DateAcquired,
                itemRequest.Quantity,
                itemRequest.UnitCost,
                resolvedClassCode,
                resolvedItemCode,
                hasClassification ? officeCode : null);

            _dbContext.PPERRItems.Add(pperrItem);

            for (int i = 0; i < itemRequest.Quantity; i++)
            {
                string propertyCode;
                string propertyNumber;

                if (hasClassification)
                {
                    var key = (ClassCode: resolvedClassCode!, ItemCode: resolvedItemCode!);
                    var counter = counters[key];
                    var seq = counter.NextSequence();

                    // Format: {YYYY}-NFA-{OFFICE}-{CLASS}-{ITEM}-{SEQ:D3}
                    propertyCode = $"{year}-NFA-{officeCode}-{resolvedClassCode}-{resolvedItemCode}-{seq:D3}";
                    propertyNumber = propertyCode;
                }
                else
                {
                    propertyCode = itemRequest.PropertyCode ?? $"PPE-{year}-{ppeItemsCreated + 1:D5}";
                    propertyNumber = propertyCode;
                }

                var ppeItem = PPEItem.Create(
                    propertyCode,
                    propertyNumber,
                    itemRequest.Description,
                    itemRequest.SerialNumber,
                    itemRequest.DateAcquired,
                    itemRequest.UnitCost,
                    itemRequest.EstimatedUsefulLifeYears,
                    pperr.Id,
                    resolvedClassCode,
                    resolvedItemCode,
                    hasClassification ? officeCode : null);

                ppeItem.CreatedBy = _currentUser.GetUserId().ToString();
                _dbContext.PPEItems.Add(ppeItem);
                ppeItemsCreated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreatePPERRResult(pperr.Id, pperr.PPERRNo, ppeItemsCreated);
    }
}
