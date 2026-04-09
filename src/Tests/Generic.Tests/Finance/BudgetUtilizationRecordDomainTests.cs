using FSH.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using FSH.Modules.Finance.Domain.BudgetUtilizationRecords;

namespace Generic.Tests.Finance;

public sealed class BudgetUtilizationRecordDomainTests
{
    [Fact]
    public void Create_WithValidData_SetsDraftStatusAndFields()
    {
        // Arrange
        var purchaseOrderId = Guid.NewGuid();
        var burDate = new DateOnly(2026, 4, 9);

        // Act
        var bur = BudgetUtilizationRecord.Create(
            burNumber: "BUR-2026-0001",
            purchaseOrderId: purchaseOrderId,
            purchaseOrderNumber: "PO-2026-0001",
            disbursementVoucherId: null,
            disbursementVoucherNumber: null,
            burDate: burDate,
            allotmentClass: "MOOE",
            uacsObjectCode: "5-02-99-990",
            responsibilityCenter: "FIN-01",
            particulars: "Office supplies",
            amount: 12500.50m,
            remarks: "Initial draft");

        // Assert
        bur.Status.ShouldBe(BudgetUtilizationRecordStatus.Draft);
        bur.BurNumber.ShouldBe("BUR-2026-0001");
        bur.PurchaseOrderId.ShouldBe(purchaseOrderId);
        bur.BurDate.ShouldBe(burDate);
        bur.Amount.ShouldBe(12500.50m);
        bur.LastModifiedOnUtc.ShouldBeNull();
    }

    [Fact]
    public void Obligate_WhenDraft_TransitionsToObligated()
    {
        // Arrange
        var bur = CreateDraftBur();

        // Act
        bur.Obligate();

        // Assert
        bur.Status.ShouldBe(BudgetUtilizationRecordStatus.Obligated);
        bur.LastModifiedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Utilize_WhenNotObligated_ThrowsInvalidOperationException()
    {
        // Arrange
        var bur = CreateDraftBur();

        // Act
        var action = () => bur.Utilize(Guid.NewGuid(), "DV-2026-0001");

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WhenUtilized_ThrowsInvalidOperationException()
    {
        // Arrange
        var bur = CreateDraftBur();
        bur.Obligate();
        bur.Utilize(Guid.NewGuid(), "DV-2026-0001");

        // Act
        var action = () => bur.Cancel("Cannot cancel utilized BUR");

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    private static BudgetUtilizationRecord CreateDraftBur() => BudgetUtilizationRecord.Create(
        burNumber: "BUR-2026-0002",
        purchaseOrderId: Guid.NewGuid(),
        purchaseOrderNumber: "PO-2026-0002",
        disbursementVoucherId: null,
        disbursementVoucherNumber: null,
        burDate: new DateOnly(2026, 4, 9),
        allotmentClass: "CO",
        uacsObjectCode: "1-07-05-010",
        responsibilityCenter: "FIN-02",
        particulars: "Equipment procurement",
        amount: 50000m,
        remarks: null);
}
