using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Expendable.Domain.Products;

/// <summary>Product status enumeration</summary>
public enum ProductStatus
{
    None = 0,
    Active = 1,
    Inactive = 2,
    Discontinued = 3,
    OutOfStock = 4
}

public class Product : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string StockNo { get; private set; } = default!;
    public string Article { get; private set; } = default!; // Generic noun/class of the item, e.g., "Paper", "Toner"
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public string UnitOfMeasure { get; private set; } = default!; // e.g., "PCS", "BOX", "KG"
    public int MinimumStockLevel { get; private set; }
    public int ReorderQuantity { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Active;
    public string? CategoryId { get; private set; }
    public string? SupplierId { get; private set; }
    // --- VARIANT PROPERTIES ---
    public Guid? ParentProductId { get; private set; }
    public string? VariantName { get; private set; } // e.g., "A4", "Long"
    // Storage keys (files under the tenant-scoped protected prefix), never a base64 blob.
    // ImageUrl = full image; ThumbnailUrl = small list thumbnail. A legacy data:…;base64 value
    // is still decoded transparently by ProductImageStorage so pre-migration rows keep rendering.
    public string? ImageUrl { get; private set; }
    public string? ThumbnailUrl { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDeletable
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation property for Entity Framework (Optional, but highly recommended)
    public virtual Product? ParentProduct { get; private set; }
    public virtual ICollection<Product> Variants { get; private set; } = new List<Product>();

    /// <summary>Factory method to create a new product</summary>
    public static Product Create(string tenantId, string stockNo, string article, string name, string description,
        decimal unitPrice, string unitOfMeasure, int minimumStockLevel, int reorderQuantity,
        string? categoryId = null, string? supplierId = null)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StockNo = stockNo,
            Article = article,
            Name = name,
            Description = description,
            UnitPrice = unitPrice,
            UnitOfMeasure = unitOfMeasure,
            MinimumStockLevel = minimumStockLevel,
            ReorderQuantity = reorderQuantity,
            CategoryId = categoryId,
            SupplierId = supplierId,
            Status = ProductStatus.Active,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Factory method to create a variant from an existing base product</summary>
    public Product CreateVariant(string stockNo, string variantName, decimal unitPrice,
        string unitOfMeasure, int minimumStockLevel, int reorderQuantity)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            TenantId = this.TenantId,
            ParentProductId = this.Id, // Link to the base product
            VariantName = variantName,
            Name = $"{this.Name} - {variantName}", // Automatically format: "Bond Paper - A4"
            Description = this.Description, // Inherit parent description
            Article = this.Article,         // Inherit parent article
            CategoryId = this.CategoryId,   // Inherit parent category
            SupplierId = this.SupplierId,   // Note: image is NOT inherited — storage keys must not be
            // shared across rows (clearing/replacing one variant would delete another's file). A variant
            // starts with no photo and can have its own uploaded via the create/update image flow.
            StockNo = stockNo,
            UnitPrice = unitPrice,
            UnitOfMeasure = unitOfMeasure,
            MinimumStockLevel = minimumStockLevel,
            ReorderQuantity = reorderQuantity,
            Status = ProductStatus.Active,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Activate the product</summary>
    public void Activate()
    {
        Status = ProductStatus.Active;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Deactivate the product</summary>
    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Mark the product as discontinued</summary>
    public void Discontinue()
    {
        Status = ProductStatus.Discontinued;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Mark the product as out of stock</summary>
    public void MarkOutOfStock()
    {
        Status = ProductStatus.OutOfStock;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Update product details</summary>
    public void Update(string name, string description, string article, decimal unitPrice,
        int minimumStockLevel, int reorderQuantity)
    {
        Name = name;
        Description = description;
        Article = article;
        UnitPrice = unitPrice;
        MinimumStockLevel = minimumStockLevel;
        ReorderQuantity = reorderQuantity;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Records the storage keys for the product's uploaded photo + thumbnail. The caller writes the
    /// files first (via ProductImageStorage), then records the keys here.
    /// </summary>
    public void SetImage(string imageKey, string? thumbnailKey)
    {
        if (string.IsNullOrWhiteSpace(imageKey)) throw new ArgumentException("Image key is required.", nameof(imageKey));
        ImageUrl = imageKey;
        ThumbnailUrl = thumbnailKey;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Clears the product's photo (both the full image and thumbnail keys).</summary>
    public void ClearImage()
    {
        ImageUrl = null;
        ThumbnailUrl = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Assign or change the product's category</summary>
    public void SetCategory(string? categoryId)
    {
        CategoryId = categoryId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Assign or change the product's supplier</summary>
    public void SetSupplier(string? supplierId)
    {
        SupplierId = supplierId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Set or rename the variant name</summary>
    public void SetVariantName(string? variantName)
    {
        VariantName = variantName;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Soft delete the product</summary>
    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}


