using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

public sealed class AssetAssignmentHistory : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;
    public Guid AssetRegistryId { get; private set; }
    public AssetAssignmentEventType EventType { get; private set; }
    public DateTimeOffset OccurredOnUtc { get; private set; }

    public string SourceDocumentType { get; private set; } = default!;
    public Guid SourceDocumentId { get; private set; }
    public string SourceDocumentNo { get; private set; } = default!;

    public Guid? FromCustodianId { get; private set; }
    public Guid? ToCustodianId { get; private set; }
    public Guid? LocationId { get; private set; }
    public string? Remarks { get; private set; }

    public static AssetAssignmentHistory Create(
        string tenantId,
        Guid assetRegistryId,
        AssetAssignmentEventType eventType,
        DateTimeOffset occurredOnUtc,
        string sourceDocumentType,
        Guid sourceDocumentId,
        string sourceDocumentNo,
        Guid? fromCustodianId,
        Guid? toCustodianId,
        Guid? locationId,
        string? remarks)
    {
        return new AssetAssignmentHistory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AssetRegistryId = assetRegistryId,
            EventType = eventType,
            OccurredOnUtc = occurredOnUtc,
            SourceDocumentType = sourceDocumentType,
            SourceDocumentId = sourceDocumentId,
            SourceDocumentNo = sourceDocumentNo,
            FromCustodianId = fromCustodianId,
            ToCustodianId = toCustodianId,
            LocationId = locationId,
            Remarks = remarks,
        };
    }
}
