using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

namespace AMIS.Modules.AssetRegister.Domain.Receiving;

/// <summary>
/// Receiving document for incoming property:
/// <list type="bullet">
/// <item><see cref="ReceivingDocumentKind.PPERR"/> — Property, Plant &amp; Equipment Receiving Report (capital assets).</item>
/// <item><see cref="ReceivingDocumentKind.SMRR"/> — Supplies and Materials Receiving Report (semi-expendable).</item>
/// </list>
/// Both kinds materialize <c>AssetRegistry</c> rows on save (one row per item quantity).
/// </summary>
public sealed class ReceivingReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    public ReceivingDocumentKind DocumentKind { get; private set; }
    public string ReportNo { get; private set; } = default!;
    public DateOnly Date { get; private set; }
    public string ReceivedFrom { get; private set; } = default!;
    public string? Address { get; private set; }
    public ReceiptType ReceiptType { get; private set; }
    public string? OtherReceiptType { get; private set; }
    public string? FundCluster { get; private set; }

    public EmployeeRef ReceivedBy { get; private set; } = default!;
    public EmployeeRef? NotedBy { get; private set; }
    public DateOnly? DateReceived { get; private set; }

    private readonly List<ReceivingReportItem> _items = [];
    public IReadOnlyCollection<ReceivingReportItem> Items => _items.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private ReceivingReport() { }

    public static ReceivingReport Create(
        string tenantId,
        ReceivingDocumentKind documentKind,
        string reportNo,
        DateOnly date,
        string receivedFrom,
        string? address,
        ReceiptType receiptType,
        string? otherReceiptType,
        string? fundCluster,
        EmployeeRef receivedBy,
        EmployeeRef? notedBy,
        DateOnly? dateReceived)
    {
        ArgumentNullException.ThrowIfNull(receivedBy);
        if (string.IsNullOrWhiteSpace(reportNo))
            throw new InvalidOperationException("Report number is required.");
        if (string.IsNullOrWhiteSpace(receivedFrom))
            throw new InvalidOperationException("ReceivedFrom is required.");
        if (receiptType == ReceiptType.Other && string.IsNullOrWhiteSpace(otherReceiptType))
            throw new InvalidOperationException("Specify OtherReceiptType when ReceiptType is Other.");

        return new ReceivingReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentKind = documentKind,
            ReportNo = reportNo,
            Date = date,
            ReceivedFrom = receivedFrom,
            Address = address,
            ReceiptType = receiptType,
            OtherReceiptType = receiptType == ReceiptType.Other ? otherReceiptType : null,
            FundCluster = fundCluster,
            ReceivedBy = receivedBy,
            NotedBy = notedBy,
            DateReceived = dateReceived,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public ReceivingReportItem AddItem(
        Guid catalogItemId,
        string? reference,
        string description,
        DateOnly acquisitionDate,
        int quantity,
        decimal unitCost,
        string? serialNo,
        string? brand,
        string? model)
    {
        var item = ReceivingReportItem.Create(
            TenantId, Id, catalogItemId, reference, description,
            acquisitionDate, quantity, unitCost,
            serialNo, brand, model);
        _items.Add(item);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        return item;
    }
}

