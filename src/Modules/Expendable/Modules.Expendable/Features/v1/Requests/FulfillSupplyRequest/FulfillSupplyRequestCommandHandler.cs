using System.Collections.ObjectModel;
using System.Net;
using FSH.Framework.Caching;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.Expendable.Data;
using FSH.Modules.Expendable.Domain.Inventory;
using FSH.Modules.Expendable.Domain.Requests;
using FSH.Modules.Expendable.Domain.Warehouse;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Expendable.Features.v1.Requests.FulfillSupplyRequest;

public sealed class FulfillSupplyRequestCommandHandler : ICommandHandler<FulfillSupplyRequestCommand, FulfillSupplyRequestResponse>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ICurrentUser _currentUser;

    public FulfillSupplyRequestCommandHandler(ExpendableDbContext dbContext, ICacheService cache, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _cache = cache;
        _currentUser = currentUser;
    }

    public async ValueTask<FulfillSupplyRequestResponse> Handle(FulfillSupplyRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _dbContext.SupplyRequests
            .FirstOrDefaultAsync(r => r.Id == command.SupplyRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Supply request {command.SupplyRequestId} not found.");

        if (request.Status != SupplyRequestStatus.Approved)
            throw new InvalidOperationException($"Supply request must be in Approved status to fulfill. Current status: {request.Status}.");

        // Use warehouse specified in command, fall back to the one stored at approval time
        var warehouseLocationId = command.WarehouseLocationId ?? request.WarehouseLocationId
            ?? throw new InvalidOperationException(
                "Warehouse location is required. Either pass it in the command or approve the request with a warehouse location first.");

        var approvedItems = request.Items.Where(i => i.ApprovedQuantity > 0).ToList();
        if (approvedItems.Count == 0)
            throw new InvalidOperationException("Supply request has no approved items to fulfill.");

        var productIds = approvedItems.Select(i => i.ProductId).ToList();

        // Load product details for report DTOs
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.SKU })
            .ToListAsync(cancellationToken);

        // Load warehouse inventory for all items
        var productInventories = await _dbContext.ProductInventories
            .Where(pi => productIds.Contains(pi.ProductId) && pi.WarehouseLocationId == warehouseLocationId)
            .ToListAsync(cancellationToken);

        // Load or prepare employee inventories
        var employeeId = request.EmployeeId;
        var existingEmployeeInventories = await _dbContext.EmployeeInventories
            .Where(ei => ei.EmployeeId == employeeId && productIds.Contains(ei.ProductId))
            .ToListAsync(cancellationToken);

        var fulfillmentDetails = new Dictionary<Guid, (int Quantity, decimal Value)>();
        var resultItems = new List<FulfillmentItemResultDto>();

        foreach (var item in approvedItems)
        {
            var productInventory = productInventories.FirstOrDefault(pi => pi.ProductId == item.ProductId)
                ?? throw new InvalidOperationException(
                    $"No warehouse inventory found for product {item.ProductId} at warehouse {warehouseLocationId}. " +
                    "Ensure the product has been received and inspected at this warehouse.");

            // Issue from warehouse FIFO batches
            Collection<IssuedBatchDetail> issuedBatches;
            try
            {
                issuedBatches = productInventory.IssueFromBatches(item.ApprovedQuantity);
            }
            catch (InvalidOperationException ex)
            {
                throw new CustomException(ex.Message, ex, HttpStatusCode.Conflict);
            }
            var totalValue = issuedBatches.Sum(b => b.TotalValue);
            var unitPrice = item.ApprovedQuantity > 0 ? Math.Round(totalValue / item.ApprovedQuantity, 4) : 0m;

            fulfillmentDetails[item.ProductId] = (item.ApprovedQuantity, totalValue);

            // Invalidate warehouse inventory cache
            await _cache.RemoveItemAsync($"inventory:{item.ProductId}:{warehouseLocationId}", cancellationToken);
            await _cache.RemoveItemAsync($"inventory:{productInventory.Id}", cancellationToken);

            // Find or create employee inventory record
            var employeeInventory = existingEmployeeInventories.FirstOrDefault(ei => ei.ProductId == item.ProductId);
            if (employeeInventory == null)
            {
                employeeInventory = EmployeeInventory.Create(request.TenantId, employeeId, item.ProductId);
                _dbContext.EmployeeInventories.Add(employeeInventory);
                existingEmployeeInventories.Add(employeeInventory);
            }

            // Record the issued stock into employee's personal inventory
            employeeInventory.ReceiveInventory(item.ApprovedQuantity, batchNumber: request.RequestNumber);

            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            resultItems.Add(new FulfillmentItemResultDto(
                item.ProductId,
                product?.Name ?? "Unknown",
                product?.SKU ?? string.Empty,
                item.ApprovedQuantity,
                unitPrice,
                totalValue
            ));
        }

        // Record fulfillment quantities/values on the request, transition to Fulfilled
        request.Fulfill(fulfillmentDetails);
        request.LastModifiedBy = _currentUser.GetUserId().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new FulfillSupplyRequestResponse(
            request.Id,
            request.RequestNumber,
            request.EmployeeId,
            request.DepartmentId,
            request.LastModifiedOnUtc ?? DateTimeOffset.UtcNow,
            resultItems,
            resultItems.Sum(i => i.TotalValue)
        );
    }
}
