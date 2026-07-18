using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;

namespace AMIS.Modules.AssetRegister.Domain.Transfers;

/// <summary>
/// One side of an inter-agency property transfer handshake.
/// <para>
/// There is no such thing as a cross-tenant row in this architecture — tenants may live in physically
/// separate databases and Finbuckle rejects saving another tenant's rows through the current context.
/// So a transfer is <b>two</b> ordinary tenant-scoped rows, one in each agency's own schema, joined by a
/// shared <see cref="CorrelationId"/>: an <see cref="TransferOfferDirection.Outbound"/> row written by the
/// sender when it posts the PPEIR, and an <see cref="TransferOfferDirection.Inbound"/> copy projected into
/// the receiving tenant by <c>AssetTransferProjector</c>. Each agency sees only its own copy.
/// </para>
/// <para>
/// The row doubles as its own outbox. The sender writes the issuance report and the outbound offer in one
/// SaveChanges (atomic, one tenant, one transaction); a recurring job later projects the inbound copy and
/// stamps <see cref="OfferProjectedUtc"/>. Re-projection is a no-op because <see cref="CorrelationId"/> is
/// uniquely indexed in the receiving tenant — so delivery is at-least-once without an inbox store.
/// </para>
/// </summary>
public sealed class AssetTransferOffer : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>The join key shared by the sender's and receiver's copies of this transfer.</summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>Which side of the handshake this row represents.</summary>
    public TransferOfferDirection Direction { get; private set; }

    public string FromTenantId { get; private set; } = default!;
    public string FromAgencyName { get; private set; } = default!;
    public string ToTenantId { get; private set; } = default!;
    public string ToAgencyName { get; private set; } = default!;

    /// <summary>The sender's PPEIR/SMIR. Meaningful only on the sender's books — the receiver keeps it as a reference.</summary>
    public Guid SourceIssuanceReportId { get; private set; }
    public string SourceIssuanceReportNo { get; private set; } = default!;
    public IssuanceReportType IssuanceReportType { get; private set; }

    public TransferOfferStatus Status { get; private set; }

    /// <summary>The receiving agency's own PPERR/SMRR, once it accepts. Null until then.</summary>
    public Guid? ReceivingReportId { get; private set; }
    public string? ReceivingReportNo { get; private set; }

    public string? RejectedReason { get; private set; }
    public DateTimeOffset? RespondedUtc { get; private set; }

    /// <summary>
    /// Outbound rows only: when the inbound copy was successfully projected into the receiving tenant.
    /// Null means the projector still owes this offer a delivery.
    /// </summary>
    public DateTimeOffset? OfferProjectedUtc { get; private set; }

    /// <summary>
    /// Inbound rows only: when this row's accept/reject response was carried back to the sending tenant.
    /// Null while a response is pending delivery.
    /// </summary>
    public DateTimeOffset? ResponseProjectedUtc { get; private set; }

    private readonly List<AssetTransferOfferLine> _lines = [];
    public IReadOnlyCollection<AssetTransferOfferLine> Lines => _lines.AsReadOnly();

    public decimal TotalUnitCost => _lines.Sum(l => l.UnitCost);
    public decimal TotalNetBookValue => _lines.Sum(l => l.NetBookValue);

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // Soft-delete columns without the ISoftDeletable marker: BaseDbContext applies an ANONYMOUS global
    // filter to ISoftDeletable entities, and EF Core 10 forbids mixing that with the NAMED filter that
    // .IsMultiTenant() registers. The named "SoftDelete" filter in the EF config enforces this instead.
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private AssetTransferOffer() { }

    /// <summary>Creates the sender's copy, written in the same transaction as the PPEIR that raised it.</summary>
    public static AssetTransferOffer CreateOutbound(
        string tenantId,
        Guid correlationId,
        string fromAgencyName,
        string toTenantId,
        string toAgencyName,
        Guid sourceIssuanceReportId,
        string sourceIssuanceReportNo,
        IssuanceReportType issuanceReportType)
    {
        if (string.IsNullOrWhiteSpace(toTenantId))
            throw new InvalidOperationException("A transfer offer requires a destination tenant.");
        if (string.Equals(tenantId, toTenantId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("An agency cannot raise an inter-agency transfer offer to itself.");

        return new AssetTransferOffer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CorrelationId = correlationId,
            Direction = TransferOfferDirection.Outbound,
            FromTenantId = tenantId,
            FromAgencyName = fromAgencyName,
            ToTenantId = toTenantId,
            ToAgencyName = toAgencyName,
            SourceIssuanceReportId = sourceIssuanceReportId,
            SourceIssuanceReportNo = sourceIssuanceReportNo,
            IssuanceReportType = issuanceReportType,
            Status = TransferOfferStatus.Sent,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates the receiver's copy. Only <c>AssetTransferProjector</c> calls this, from inside a DI scope
    /// whose ambient tenant has been switched to <paramref name="tenantId"/>.
    /// </summary>
    public static AssetTransferOffer CreateInbound(
        string tenantId,
        Guid correlationId,
        string fromTenantId,
        string fromAgencyName,
        string toAgencyName,
        Guid sourceIssuanceReportId,
        string sourceIssuanceReportNo,
        IssuanceReportType issuanceReportType,
        DateTimeOffset offeredOnUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CorrelationId = correlationId,
            Direction = TransferOfferDirection.Inbound,
            FromTenantId = fromTenantId,
            FromAgencyName = fromAgencyName,
            ToTenantId = tenantId,
            ToAgencyName = toAgencyName,
            SourceIssuanceReportId = sourceIssuanceReportId,
            SourceIssuanceReportNo = sourceIssuanceReportNo,
            IssuanceReportType = issuanceReportType,
            Status = TransferOfferStatus.Sent,
            CreatedOnUtc = offeredOnUtc
        };

    public AssetTransferOfferLine AddLine(
        string sourcePropertyNo,
        string description,
        string? serialNo,
        string? brand,
        string? model,
        decimal unitCost,
        DateOnly originalAcquisitionDate,
        decimal accumulatedDepreciation,
        DateOnly? depreciationCurrentThrough,
        decimal netBookValue,
        string? catalogUacsCode)
    {
        var line = AssetTransferOfferLine.Create(
            TenantId, Id, _lines.Count + 1, sourcePropertyNo, description,
            serialNo, brand, model, unitCost, originalAcquisitionDate,
            accumulatedDepreciation, depreciationCurrentThrough, netBookValue, catalogUacsCode);
        _lines.Add(line);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        return line;
    }

    /// <summary>
    /// Records the receiving agency's acceptance, linking the PPERR/SMRR it posted on its own form series.
    /// The receiving report is the receiver's document — the sender only ever learns its number.
    /// </summary>
    public void Accept(Guid receivingReportId, string receivingReportNo)
    {
        EnsureRespondable(nameof(Accept));
        if (string.IsNullOrWhiteSpace(receivingReportNo))
            throw new InvalidOperationException("A receiving report number is required to accept a transfer offer.");

        Status = TransferOfferStatus.Accepted;
        ReceivingReportId = receivingReportId;
        ReceivingReportNo = receivingReportNo;
        RespondedUtc = DateTimeOffset.UtcNow;
        ResponseProjectedUtc = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Reject(string reason)
    {
        EnsureRespondable(nameof(Reject));
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("A reason is required to reject a transfer offer.");

        Status = TransferOfferStatus.Rejected;
        RejectedReason = reason.Trim();
        RespondedUtc = DateTimeOffset.UtcNow;
        ResponseProjectedUtc = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Withdraws an offer the receiver has not yet answered. Note this does NOT return the assets to the
    /// sender's active balance — the PPEIR already moved them to TransferredOut, exactly as the paper
    /// process does. Reversing that is a separate accountable document.
    /// </summary>
    public void Cancel()
    {
        EnsureRespondable(nameof(Cancel));
        Status = TransferOfferStatus.Cancelled;
        RespondedUtc = DateTimeOffset.UtcNow;
        ResponseProjectedUtc = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Stamped on the sender's row once the inbound copy has landed in the receiving tenant.</summary>
    public void MarkOfferProjected()
    {
        OfferProjectedUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Stamped on the receiver's row once its response has been carried back to the sender.</summary>
    public void MarkResponseProjected()
    {
        ResponseProjectedUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Applies the counterpart tenant's response onto this row. Called by the projector on the sender's
    /// outbound copy. Idempotent — replaying a response that already landed is a no-op, which is what makes
    /// the at-least-once carry-back safe.
    /// </summary>
    public void ApplyResponse(
        TransferOfferStatus status,
        Guid? receivingReportId,
        string? receivingReportNo,
        string? rejectedReason,
        DateTimeOffset respondedUtc)
    {
        if (Status == status)
            return;
        if (Status != TransferOfferStatus.Sent)
            throw new InvalidOperationException(
                $"Transfer offer '{CorrelationId}' is already '{Status}' and cannot record a '{status}' response.");
        if (status == TransferOfferStatus.Sent)
            throw new InvalidOperationException("'Sent' is not a response status.");

        Status = status;
        ReceivingReportId = receivingReportId;
        ReceivingReportNo = receivingReportNo;
        RejectedReason = rejectedReason;
        RespondedUtc = respondedUtc;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    private void EnsureRespondable(string action)
    {
        if (Status != TransferOfferStatus.Sent)
            throw new InvalidOperationException(
                $"Cannot {action} transfer offer '{CorrelationId}': it is already '{Status}'.");
    }
}
