using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Domain.Transfers;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

/// <summary>
/// State machine of the two-row inter-agency transfer handshake. Both agencies hold their own copy of the
/// offer joined by a correlation id; the receiver answers on its copy and the projector replays that answer
/// onto the sender's, so the transitions here have to be strict AND the replay has to be idempotent.
/// </summary>
public sealed class AssetTransferOfferTests
{
    [Fact]
    public void CreateOutbound_StartsSentAndUndelivered()
    {
        var offer = NewOutbound();

        offer.Status.ShouldBe(TransferOfferStatus.Sent);
        offer.Direction.ShouldBe(TransferOfferDirection.Outbound);
        offer.FromTenantId.ShouldBe("agency-a");
        offer.ToTenantId.ShouldBe("agency-b");
        offer.OfferProjectedUtc.ShouldBeNull();   // the projector still owes this one a delivery
        offer.RespondedUtc.ShouldBeNull();
    }

    [Fact]
    public void CreateOutbound_RejectsSelfTransfer()
    {
        Should.Throw<InvalidOperationException>(() => AssetTransferOffer.CreateOutbound(
            tenantId: "agency-a", correlationId: Guid.NewGuid(), fromAgencyName: "Agency A",
            toTenantId: "agency-a", toAgencyName: "Agency A",
            sourceIssuanceReportId: Guid.NewGuid(), sourceIssuanceReportNo: "PPEIR-001",
            issuanceReportType: IssuanceReportType.PPEIR));
    }

    [Fact]
    public void CreateOutbound_RejectsMissingDestination()
    {
        Should.Throw<InvalidOperationException>(() => AssetTransferOffer.CreateOutbound(
            tenantId: "agency-a", correlationId: Guid.NewGuid(), fromAgencyName: "Agency A",
            toTenantId: "  ", toAgencyName: "Agency B",
            sourceIssuanceReportId: Guid.NewGuid(), sourceIssuanceReportNo: "PPEIR-001",
            issuanceReportType: IssuanceReportType.PPEIR));
    }

    [Fact]
    public void AddLine_NormalizesPropertyNoAndNumbersItems()
    {
        var offer = NewOutbound();

        offer.AddLine(" 2026-nfa-00b-07-dsk-001 ", "Office Desk", null, null, null,
            unitCost: 60_000m, originalAcquisitionDate: new DateOnly(2022, 1, 15),
            accumulatedDepreciation: 45_600m, depreciationCurrentThrough: new DateOnly(2026, 1, 1),
            netBookValue: 14_400m, catalogUacsCode: "10405030");
        offer.AddLine("2026-NFA-00B-07-DSK-002", "Office Desk", null, null, null,
            unitCost: 60_000m, originalAcquisitionDate: new DateOnly(2022, 1, 15),
            accumulatedDepreciation: 45_600m, depreciationCurrentThrough: new DateOnly(2026, 1, 1),
            netBookValue: 14_400m, catalogUacsCode: "10405030");

        offer.Lines.Count.ShouldBe(2);
        offer.Lines.First().SourcePropertyNo.ShouldBe("2026-NFA-00B-07-DSK-001");
        offer.Lines.First().ItemNo.ShouldBe(1);
        offer.Lines.Last().ItemNo.ShouldBe(2);
        offer.TotalUnitCost.ShouldBe(120_000m);
        offer.TotalNetBookValue.ShouldBe(28_800m);
    }

    [Fact]
    public void AddLine_RejectsAccumulatedDepreciationAboveUnitCost()
    {
        var offer = NewOutbound();

        Should.Throw<InvalidOperationException>(() => offer.AddLine(
            "2026-NFA-00B-07-DSK-001", "Office Desk", null, null, null,
            unitCost: 60_000m, originalAcquisitionDate: new DateOnly(2022, 1, 15),
            accumulatedDepreciation: 60_000.01m, depreciationCurrentThrough: new DateOnly(2026, 1, 1),
            netBookValue: 0m, catalogUacsCode: null));
    }

    [Fact]
    public void Accept_RecordsTheReceivingReportAndQueuesTheResponseForCarryBack()
    {
        var offer = NewInbound();
        var reportId = Guid.NewGuid();

        offer.Accept(reportId, "PPERR-2026-0042");

        offer.Status.ShouldBe(TransferOfferStatus.Accepted);
        offer.ReceivingReportId.ShouldBe(reportId);
        offer.ReceivingReportNo.ShouldBe("PPERR-2026-0042");
        offer.RespondedUtc.ShouldNotBeNull();
        offer.ResponseProjectedUtc.ShouldBeNull();  // the job will pick this up
    }

    [Fact]
    public void Accept_RequiresAReceivingReportNumber()
    {
        var offer = NewInbound();
        Should.Throw<InvalidOperationException>(() => offer.Accept(Guid.NewGuid(), "  "));
    }

    [Fact]
    public void Reject_RecordsTheReason()
    {
        var offer = NewInbound();

        offer.Reject("  Serial numbers do not match the shipment.  ");

        offer.Status.ShouldBe(TransferOfferStatus.Rejected);
        offer.RejectedReason.ShouldBe("Serial numbers do not match the shipment.");
        offer.RespondedUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var offer = NewInbound();
        Should.Throw<InvalidOperationException>(() => offer.Reject("   "));
    }

    [Fact]
    public void DoubleAccept_IsRejected()
    {
        var offer = NewInbound();
        offer.Accept(Guid.NewGuid(), "PPERR-2026-0042");

        Should.Throw<InvalidOperationException>(() => offer.Accept(Guid.NewGuid(), "PPERR-2026-0043"));
    }

    [Fact]
    public void AcceptAfterReject_IsRejected()
    {
        var offer = NewInbound();
        offer.Reject("Not ours.");

        Should.Throw<InvalidOperationException>(() => offer.Accept(Guid.NewGuid(), "PPERR-2026-0042"));
    }

    [Fact]
    public void RejectAfterAccept_IsRejected()
    {
        var offer = NewInbound();
        offer.Accept(Guid.NewGuid(), "PPERR-2026-0042");

        Should.Throw<InvalidOperationException>(() => offer.Reject("Changed our mind."));
    }

    [Fact]
    public void CancelAfterResponse_IsRejected()
    {
        var offer = NewOutbound();
        offer.ApplyResponse(TransferOfferStatus.Accepted, Guid.NewGuid(), "PPERR-2026-0042", null, DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(offer.Cancel);
    }

    [Fact]
    public void ApplyResponse_CopiesTheReceiversDecisionOntoTheSendersRow()
    {
        var offer = NewOutbound();
        var reportId = Guid.NewGuid();
        var respondedUtc = DateTimeOffset.UtcNow;

        offer.ApplyResponse(TransferOfferStatus.Accepted, reportId, "PPERR-2026-0042", null, respondedUtc);

        offer.Status.ShouldBe(TransferOfferStatus.Accepted);
        offer.ReceivingReportNo.ShouldBe("PPERR-2026-0042");
        offer.RespondedUtc.ShouldBe(respondedUtc);
    }

    /// <summary>
    /// Delivery is at-least-once, so the same response WILL be replayed. Replaying it must be a no-op
    /// rather than a throw, or a duplicate carry-back would poison the projection job.
    /// </summary>
    [Fact]
    public void ApplyResponse_IsIdempotent_WhenReplayed()
    {
        var offer = NewOutbound();
        var reportId = Guid.NewGuid();
        var respondedUtc = DateTimeOffset.UtcNow;

        offer.ApplyResponse(TransferOfferStatus.Accepted, reportId, "PPERR-2026-0042", null, respondedUtc);
        offer.ApplyResponse(TransferOfferStatus.Accepted, reportId, "PPERR-2026-0042", null, respondedUtc);

        offer.Status.ShouldBe(TransferOfferStatus.Accepted);
        offer.RespondedUtc.ShouldBe(respondedUtc);
    }

    [Fact]
    public void ApplyResponse_RejectsAConflictingSecondDecision()
    {
        var offer = NewOutbound();
        offer.ApplyResponse(TransferOfferStatus.Accepted, Guid.NewGuid(), "PPERR-2026-0042", null, DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() =>
            offer.ApplyResponse(TransferOfferStatus.Rejected, null, null, "Too late.", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkOfferProjected_StampsDelivery()
    {
        var offer = NewOutbound();
        offer.MarkOfferProjected();
        offer.OfferProjectedUtc.ShouldNotBeNull();
    }

    [Fact]
    public void MarkResponseProjected_StampsCarryBack()
    {
        var offer = NewInbound();
        offer.Accept(Guid.NewGuid(), "PPERR-2026-0042");
        offer.MarkResponseProjected();
        offer.ResponseProjectedUtc.ShouldNotBeNull();
    }

    private static AssetTransferOffer NewOutbound() =>
        AssetTransferOffer.CreateOutbound(
            tenantId: "agency-a", correlationId: Guid.NewGuid(), fromAgencyName: "Agency A",
            toTenantId: "agency-b", toAgencyName: "Agency B",
            sourceIssuanceReportId: Guid.NewGuid(), sourceIssuanceReportNo: "PPEIR-2026-0007",
            issuanceReportType: IssuanceReportType.PPEIR);

    private static AssetTransferOffer NewInbound() =>
        AssetTransferOffer.CreateInbound(
            tenantId: "agency-b", correlationId: Guid.NewGuid(), fromTenantId: "agency-a",
            fromAgencyName: "Agency A", toAgencyName: "Agency B",
            sourceIssuanceReportId: Guid.NewGuid(), sourceIssuanceReportNo: "PPEIR-2026-0007",
            issuanceReportType: IssuanceReportType.PPEIR, offeredOnUtc: DateTimeOffset.UtcNow);
}
