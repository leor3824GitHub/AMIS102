using AMIS.Modules.Expendable.Domain.Purchases;

namespace Generic.Tests.Expendable;

public sealed class PurchaseTests
{
    [Fact]
    public void RecordReceipt_WhenPurchaseIsNotApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var purchase = Purchase.Create("tenant-1", "PO-123", "SUP-1", "Supplier 1", Guid.NewGuid(), "Main Warehouse");
        var productId = Guid.NewGuid();
        purchase.AddLineItem(productId, "PROD-123", "Product Name", 5, 2.5m);

        // Act
        var action = () => purchase.RecordReceipt(productId, 1);

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Submit_WhenNoLineItems_ThrowsInvalidOperationException()
    {
        // Arrange
        var purchase = Purchase.Create("tenant-1", "PO-123", "SUP-1", "Supplier 1", Guid.NewGuid(), "Main Warehouse");

        // Act
        var action = purchase.Submit;

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void RecordReceipt_WhenReceivedQuantityExceedsOrdered_ThrowsInvalidOperationException()
    {
        // Arrange
        var purchase = Purchase.Create("tenant-1", "PO-123", "SUP-1", "Supplier 1", Guid.NewGuid(), "Main Warehouse");
        var productId = Guid.NewGuid();
        purchase.AddLineItem(productId, "PROD-123", "Product Name", 5, 2.5m);
        purchase.Submit();
        purchase.Approve();

        // Act
        var action = () => purchase.RecordReceipt(productId, 6);

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }
}

