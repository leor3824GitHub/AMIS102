using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Expendable.Domain.Inventory;

/// <summary>Inventory batch for receipt traceability</summary>
public class EmployeeStockBatch
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityReceived { get; set; }
    public int QuantityConsumed { get; set; }
    public int QuantityAvailable => QuantityReceived - QuantityConsumed;
    public DateTimeOffset ReceivedOnUtc { get; set; }
    public string? BatchNumber { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }

    private EmployeeStockBatch()
    {
    }

    public EmployeeStockBatch(Guid productId, int quantity, string? batchNumber = null, DateTimeOffset? expiryDate = null)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        QuantityReceived = quantity;
        QuantityConsumed = 0;
        ReceivedOnUtc = DateTimeOffset.UtcNow;
        BatchNumber = batchNumber;
        ExpiryDate = expiryDate;
    }

    public int Consume(int quantity)
    {
        if (quantity > QuantityAvailable)
            throw new InvalidOperationException($"Cannot consume {quantity} units. Only {QuantityAvailable} available.");

        QuantityConsumed += quantity;
        return quantity;
    }

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate < DateTimeOffset.UtcNow;
}

public class EmployeeInventory : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string EmployeeId { get; private set; } = default!;
    public Guid ProductId { get; private set; }
    public int TotalQuantityReceived { get; set; }
    public int TotalQuantityConsumed { get; set; }
    public int QuantityOnHand => TotalQuantityReceived - TotalQuantityConsumed;
    // Optimistic concurrency via the Postgres system column xmin (mapped in EmployeeInventoryConfiguration).

    private readonly List<EmployeeStockBatch> _batches = [];
    public IReadOnlyCollection<EmployeeStockBatch> Batches => _batches.AsReadOnly();

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    /// <summary>Factory method to create employee inventory</summary>
    public static EmployeeInventory Create(string tenantId, string employeeId, Guid productId)
    {
        return new EmployeeInventory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ProductId = productId,
            TotalQuantityReceived = 0,
            TotalQuantityConsumed = 0,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Receive inventory (adds a new batch)</summary>
    public void ReceiveInventory(int quantity, string? batchNumber = null, DateTimeOffset? expiryDate = null)
    {
        var batch = new EmployeeStockBatch(ProductId, quantity, batchNumber, expiryDate);
        _batches.Add(batch);
        TotalQuantityReceived += quantity;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Consume inventory from available employee stock</summary>
    public int ConsumeInventory(int quantity)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Consumption quantity must be greater than zero.");

        if (QuantityOnHand < quantity)
            throw new InvalidOperationException($"Insufficient inventory. Only {QuantityOnHand} available.");

        var remainingToConsume = quantity;
        foreach (var batch in _batches.Where(b => b.QuantityAvailable > 0))
        {
            var toConsume = Math.Min(remainingToConsume, batch.QuantityAvailable);
            batch.Consume(toConsume);
            remainingToConsume -= toConsume;

            if (remainingToConsume == 0)
                break;
        }

        TotalQuantityConsumed += quantity;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        return quantity;
    }

    /// <summary>Get available batches for consumption</summary>
    public IEnumerable<EmployeeStockBatch> GetAvailableBatches() =>
        _batches.Where(b => b.QuantityAvailable > 0 && !b.IsExpired);

    /// <summary>Get expired batches</summary>
    public IEnumerable<EmployeeStockBatch> GetExpiredBatches() =>
        _batches.Where(b => b.IsExpired);
}

/// <summary>Audit entity for inventory consumption tracking</summary>
public class InventoryConsumption : BaseEntity<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; set; } = default!;
    public Guid EmployeeInventoryId { get; set; }
    public Guid ProductId { get; set; }
    public string EmployeeId { get; set; } = default!;
    public int QuantityConsumed { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceNumber { get; set; } // Reference to request/supply order
    public DateTimeOffset ConsumptionDate { get; set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public static InventoryConsumption Create(string tenantId, Guid employeeInventoryId, Guid productId,
        string employeeId, int quantity, string? reason = null, string? referenceNumber = null)
    {
        return new InventoryConsumption
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeInventoryId = employeeInventoryId,
            ProductId = productId,
            EmployeeId = employeeId,
            QuantityConsumed = quantity,
            Reason = reason,
            ReferenceNumber = referenceNumber,
            ConsumptionDate = DateTimeOffset.UtcNow,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }
}


