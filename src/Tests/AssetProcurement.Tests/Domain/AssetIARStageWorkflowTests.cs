using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.AssetProcurement.Domain.AssetInspectionAcceptanceReports;
using Shouldly;
using Xunit;

namespace AssetProcurement.Tests.Domain;

public sealed class AssetIARStageWorkflowTests
{
    [Fact]
    public void SubmitForInspection_FromDraft_TransitionsToPendingInspection()
    {
        var iar = NewDraft(NewLine("Desk"));

        iar.SubmitForInspection();

        iar.Status.ShouldBe(AssetIARStatus.PendingInspection);
        iar.SubmittedForInspectionOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void SubmitForInspection_WithNoLines_Throws()
    {
        var iar = NewDraft();

        Should.Throw<InvalidOperationException>(() => iar.SubmitForInspection())
            .Message.ShouldContain("at least one line item");
    }

    [Fact]
    public void SubmitForInspection_FromNonDraft_Throws()
    {
        var iar = NewDraft(NewLine("Desk"));
        iar.SubmitForInspection();

        Should.Throw<InvalidOperationException>(() => iar.SubmitForInspection());
    }

    [Fact]
    public void ReassignInspector_FromPendingInspection_UpdatesInspector()
    {
        var iar = NewDraft(NewLine("Desk"));
        iar.SubmitForInspection();
        var newInspector = Guid.NewGuid();

        iar.ReassignInspector(newInspector);

        iar.InspectedById.ShouldBe(newInspector);
    }

    [Fact]
    public void ReassignInspector_FromDraft_Throws() =>
        Should.Throw<InvalidOperationException>(() => NewDraft(NewLine("Desk")).ReassignInspector(Guid.NewGuid()));

    [Fact]
    public void ReassignInspector_WithEmptyGuid_Throws()
    {
        var iar = NewDraft(NewLine("Desk"));
        iar.SubmitForInspection();

        Should.Throw<InvalidOperationException>(() => iar.ReassignInspector(Guid.Empty));
    }

    [Fact]
    public void RecordInspection_AllPassed_TransitionsToInspected()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewDraftWithInspector(inspectorId, NewLine("Desk"), NewLine("Chair"));
        iar.SubmitForInspection();

        iar.RecordInspection(inspectorId, [
            new LineInspectionDecision(1, LineInspectionResult.Passed, null),
            new LineInspectionDecision(2, LineInspectionResult.Passed, null),
        ]);

        iar.Status.ShouldBe(AssetIARStatus.Inspected);
        iar.InspectedOnUtc.ShouldNotBeNull();
        iar.LineItems.ShouldAllBe(li => li.InspectionResult == LineInspectionResult.Passed);
        iar.LineItems.ShouldAllBe(li => li.InspectedById == inspectorId);
    }

    [Fact]
    public void RecordInspection_ByWrongActor_ThrowsUnauthorized()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewDraftWithInspector(inspectorId, NewLine("Desk"));
        iar.SubmitForInspection();

        Should.Throw<UnauthorizedAccessException>(() =>
            iar.RecordInspection(Guid.NewGuid(),
                [new LineInspectionDecision(1, LineInspectionResult.Passed, null)]));
    }

    [Fact]
    public void RecordInspection_RejectedLineWithoutRemarks_Throws()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewDraftWithInspector(inspectorId, NewLine("Desk"));
        iar.SubmitForInspection();

        Should.Throw<InvalidOperationException>(() =>
            iar.RecordInspection(inspectorId,
                [new LineInspectionDecision(1, LineInspectionResult.Rejected, "")]))
            .Message.ShouldContain("remarks are required");
    }

    [Fact]
    public void RecordInspection_MissingDecisionForLine_Throws()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewDraftWithInspector(inspectorId, NewLine("Desk"), NewLine("Chair"));
        iar.SubmitForInspection();

        Should.Throw<InvalidOperationException>(() =>
            iar.RecordInspection(inspectorId,
                [new LineInspectionDecision(1, LineInspectionResult.Passed, null)]))
            .Message.ShouldContain("Missing on item(s): 2");
    }

    [Fact]
    public void RecordInspection_FromNonPending_Throws()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewDraftWithInspector(inspectorId, NewLine("Desk"));

        Should.Throw<InvalidOperationException>(() =>
            iar.RecordInspection(inspectorId,
                [new LineInspectionDecision(1, LineInspectionResult.Passed, null)]));
    }

    [Fact]
    public void AssignPropertyNo_OnPassedLine_Normalizes()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewInspected(inspectorId, (NewLine("Desk"), LineInspectionResult.Passed));

        iar.AssignPropertyNo(1, "  2026-nfa-00b-07-dsk-001  ");

        iar.LineItems[0].StockPropertyNo.ShouldBe("2026-NFA-00B-07-DSK-001");
    }

    [Fact]
    public void AssignPropertyNo_OnRejectedLine_Throws()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewInspected(inspectorId, (NewLine("Desk"), LineInspectionResult.Rejected));

        Should.Throw<InvalidOperationException>(() => iar.AssignPropertyNo(1, "X-001"));
    }

    [Fact]
    public void AssignPropertyNo_FromDraftStatus_Throws()
    {
        var iar = NewDraft(NewLine("Desk"));

        Should.Throw<InvalidOperationException>(() => iar.AssignPropertyNo(1, "X-001"));
    }

    [Fact]
    public void Accept_FromInspected_SkipsRejectedLinesForPropertyNoCheck()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewInspected(inspectorId,
            (NewLine("Desk"), LineInspectionResult.Passed),
            (NewLine("Chair"), LineInspectionResult.Rejected));
        iar.AssignPropertyNo(1, "DSK-001");

        Should.NotThrow(() => iar.Accept());
        iar.Status.ShouldBe(AssetIARStatus.Accepted);
        iar.AcceptedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Accept_FromInspected_FailsWhenPassedLineMissingPropertyNo()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewInspected(inspectorId,
            (NewLine("Desk"), LineInspectionResult.Passed),
            (NewLine("Chair"), LineInspectionResult.Passed));
        iar.AssignPropertyNo(1, "DSK-001");

        Should.Throw<InvalidOperationException>(() => iar.Accept())
            .Message.ShouldContain("Missing on item(s): 2");
    }

    [Fact]
    public void Accept_FromCancelled_Throws()
    {
        var iar = NewDraft(NewLine("Desk"));
        iar.Cancel();

        Should.Throw<InvalidOperationException>(() => iar.Accept());
    }

    [Fact]
    public void ExpandLineByQuantity_OnPassedLineQty3_ProducesThreeLinesOfQty1()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewInspected(inspectorId,
            (NewLine("Desk", quantity: 3m), LineInspectionResult.Passed));

        iar.ExpandLineByQuantity(1);

        iar.LineItems.Count.ShouldBe(3);
        iar.LineItems.ShouldAllBe(li => li.Quantity == 1m);
        iar.LineItems.Select(li => li.ItemNo).ShouldBe([1, 2, 3]);
        iar.LineItems.ShouldAllBe(li => li.InspectionResult == LineInspectionResult.Passed);
        iar.LineItems.ShouldAllBe(li => string.IsNullOrEmpty(li.StockPropertyNo));
    }

    [Fact]
    public void ExpandLineByQuantity_OnQty1Line_Throws()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewInspected(inspectorId,
            (NewLine("Desk", quantity: 1m), LineInspectionResult.Passed));

        Should.Throw<InvalidOperationException>(() => iar.ExpandLineByQuantity(1));
    }

    [Fact]
    public void ExpandLineByQuantity_OnRejectedLine_Throws()
    {
        var inspectorId = Guid.NewGuid();
        var iar = NewInspected(inspectorId,
            (NewLine("Desk", quantity: 3m), LineInspectionResult.Rejected));

        Should.Throw<InvalidOperationException>(() => iar.ExpandLineByQuantity(1));
    }

    [Fact]
    public void Cancel_FromDraft_TransitionsToCancelled()
    {
        var iar = NewDraft(NewLine("Desk"));

        iar.Cancel();

        iar.Status.ShouldBe(AssetIARStatus.Cancelled);
        iar.CancelledOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Cancel_FromPendingInspection_TransitionsToCancelled()
    {
        var iar = NewDraft(NewLine("Desk"));
        iar.SubmitForInspection();

        iar.Cancel();

        iar.Status.ShouldBe(AssetIARStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromAccepted_Throws()
    {
        var iar = NewDraft(NewLine("Desk", stockPropertyNo: "X-001"));
        iar.Accept();

        Should.Throw<InvalidOperationException>(() => iar.Cancel());
    }

    [Fact]
    public void LegacyDirectAccept_FromDraft_StillWorks()
    {
        // Back-compat: legacy flow where the IAR is created and accepted without inspection stage.
        // Lines have InspectionResult.Pending; Accept treats Pending the same as Passed for PropertyNo validation.
        var iar = NewDraft(
            NewLine("Desk", stockPropertyNo: "DSK-001"),
            NewLine("Chair", stockPropertyNo: "CHR-001"));

        Should.NotThrow(() => iar.Accept());
        iar.Status.ShouldBe(AssetIARStatus.Accepted);
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private static AssetIARLineItemRequest NewLine(
        string description, decimal quantity = 1m, decimal unitCost = 1000m, string? stockPropertyNo = null) =>
        new(description, null, null, null, null, null, "pc", quantity, unitCost, null, stockPropertyNo);

    private static AssetInspectionAcceptanceReport NewDraft(params AssetIARLineItemRequest[] lines) =>
        NewDraftWithInspector(Guid.NewGuid(), lines);

    private static AssetInspectionAcceptanceReport NewDraftWithInspector(
        Guid inspectorId, params AssetIARLineItemRequest[] lines) =>
        AssetInspectionAcceptanceReport.Create(
            tenantId: "root",
            iarNumber: "IAR-2026-0001",
            purchaseOrderId: Guid.NewGuid(),
            supplierId: Guid.NewGuid(),
            supplierName: "ACME Office Supplies",
            inspectedById: inspectorId,
            receivedById: Guid.NewGuid(),
            deliveryReceiptNo: "DR-001",
            deliveryDate: new DateOnly(2026, 5, 14),
            remarks: null,
            lineItems: lines);

    private static AssetInspectionAcceptanceReport NewInspected(
        Guid inspectorId,
        params (AssetIARLineItemRequest line, LineInspectionResult result)[] lines)
    {
        var iar = NewDraftWithInspector(inspectorId, lines.Select(l => l.line).ToArray());
        iar.SubmitForInspection();
        var decisions = lines.Select((l, idx) => new LineInspectionDecision(
            idx + 1, l.result,
            l.result == LineInspectionResult.Rejected ? "Failed inspection" : null)).ToList();
        iar.RecordInspection(inspectorId, decisions);
        return iar;
    }
}
