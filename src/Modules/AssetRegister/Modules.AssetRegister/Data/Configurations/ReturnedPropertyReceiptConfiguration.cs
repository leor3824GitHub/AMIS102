using AMIS.Framework.Persistence.SignedDocuments;
using AMIS.Modules.AssetRegister.Domain.ReturnedProperty;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class ReturnedPropertyReceiptConfiguration : IEntityTypeConfiguration<ReturnedPropertyReceipt>
{
    public void Configure(EntityTypeBuilder<ReturnedPropertyReceipt> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ReturnedPropertyReceipts", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.ConfigureSignedCopy(x => x.SignedCopy);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        // Null while Pending; assigned on acceptance. Postgres treats NULLs as distinct, so the unique
        // index still guards against duplicate assigned numbers without blocking multiple pending requests.
        builder.Property(x => x.ReceiptNo).HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.AccountabilityDocumentNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.InspectionRemarks).HasMaxLength(1000);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);

        builder.OwnsOne(x => x.ReturnedBy, n => n.ConfigureEmployeeRef("ReturnedBy"));
        builder.OwnsOne(x => x.AssignedInspector, n => n.ConfigureEmployeeRef("AssignedInspector"));
        builder.OwnsOne(x => x.InspectedBy, n => n.ConfigureEmployeeRef("InspectedBy"));
        builder.OwnsOne(x => x.ReceivedBy, n => n.ConfigureEmployeeRef("ReceivedBy"));
        builder.Navigation(x => x.ReturnedBy).IsRequired();
        builder.Navigation(x => x.AssignedInspector).IsRequired();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(i => i.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).AutoInclude(false);

        builder.HasIndex(x => new { x.TenantId, x.ReceiptNo }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.Date });
        builder.HasIndex(x => new { x.TenantId, x.AccountabilityId });
    }
}

internal sealed class ReturnedPropertyReceiptItemConfiguration : IEntityTypeConfiguration<ReturnedPropertyReceiptItem>
{
    public void Configure(EntityTypeBuilder<ReturnedPropertyReceiptItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ReturnedPropertyReceiptItems", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.InspectedCondition).HasConversion<int>();

        builder.OwnsOne(x => x.Snapshot, n => n.ConfigureSnapshot());
        builder.Navigation(x => x.Snapshot).IsRequired();

        builder.HasIndex(x => x.AssetRegistryId);
        builder.HasIndex(x => x.AccountabilityLineId);
    }
}
