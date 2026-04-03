using FSH.Framework.Core.Domain;

namespace FSH.Modules.Expendable.Domain.Cart;

/// <summary>Shopping cart status enumeration</summary>
public enum CartStatus
{
    None = 0,
    Active = 1,
    Converted = 2,
    Abandoned = 3,
    Cleared = 4
}

/// <summary>Cart item value object</summary>
public class CartItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTimeOffset AddedOnUtc { get; set; }

    public CartItem(Guid productId, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        AddedOnUtc = DateTimeOffset.UtcNow;
    }

    public decimal LineTotal => Quantity * UnitPrice;

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Cart item quantity must be greater than zero.");

        Quantity = quantity;
    }
}

public class EmployeeShoppingCart : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string EmployeeId { get; private set; } = default!;
    public CartStatus Status { get; set; } = CartStatus.Active;
    public DateTimeOffset? ConvertedOnUtc { get; set; }
    public Guid? ConvertedToRequestId { get; set; }
    public byte[] Version { get; set; } = [];

    private readonly List<CartItem> _items = [];
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDeletable
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Factory method to create a new shopping cart</summary>
    public static EmployeeShoppingCart Create(string tenantId, string employeeId)
    {
        return new EmployeeShoppingCart
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Status = CartStatus.Active,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Add an item to the cart.</summary>
    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot add items to an inactive cart.");

        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");

        var existingItem = _items.FirstOrDefault(x => x.ProductId == productId && x.UnitPrice == unitPrice);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            _items.Add(new CartItem(productId, quantity, unitPrice));
        }

        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Remove all rows for a product from the cart.</summary>
    public void RemoveItem(Guid productId)
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot modify an inactive cart.");

        _items.RemoveAll(x => x.ProductId == productId);

        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Update item quantity in the cart</summary>
    public void UpdateItemQuantity(Guid productId, int newQuantity)
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot modify an inactive cart.");

        var item = _items.FirstOrDefault(x => x.ProductId == productId);
        if (item == null)
            throw new InvalidOperationException($"Product {productId} not found in cart.");

        if (newQuantity <= 0)
            RemoveItem(productId);
        else
            item.UpdateQuantity(newQuantity);

        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Get cart total amount</summary>
    public decimal GetCartTotal() => _items.Sum(x => x.LineTotal);

    /// <summary>Get total number of items in cart</summary>
    public int GetTotalItemCount() => _items.Sum(x => x.Quantity);

    /// <summary>Convert cart to supply request</summary>
    public void ConvertToRequest(Guid supplyRequestId)
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Only active carts can be converted.");

        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot convert an empty cart.");

        Status = CartStatus.Converted;
        ConvertedOnUtc = DateTimeOffset.UtcNow;
        ConvertedToRequestId = supplyRequestId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Clear the cart</summary>
    public void Clear()
    {
        _items.Clear();
        Status = CartStatus.Cleared;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Soft delete the cart</summary>
    public void SoftDelete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}

