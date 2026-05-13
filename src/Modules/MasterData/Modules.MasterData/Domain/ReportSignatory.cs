using System.Security.Cryptography;
using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.MasterData.Domain;

public sealed class ReportSignatory : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string ReportType { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public string Label { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public byte[] Version { get; set; } = [];

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static ReportSignatory Create(string tenantId, string reportType, int sortOrder, string label, string name, string title)
    {
        return new ReportSignatory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportType = reportType,
            SortOrder = sortOrder,
            Label = label,
            Name = name,
            Title = title,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };
    }

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);

    public void Update(string reportType, int sortOrder, string label, string name, string title, bool isActive)
    {
        ReportType = reportType;
        SortOrder = sortOrder;
        Label = label;
        Name = name;
        Title = title;
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void Delete(string deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
        Version = NewVersion();
    }
}

