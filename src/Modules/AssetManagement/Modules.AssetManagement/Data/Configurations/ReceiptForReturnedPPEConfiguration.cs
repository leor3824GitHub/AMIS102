using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class ReceiptForReturnedPPEConfiguration : IEntityTypeConfiguration<ReceiptForReturnedPPE>
{
    public void Configure(EntityTypeBuilder<ReceiptForReturnedPPE> builder)
    {
        builder.ToTable("ReceiptsForReturnedPPE", AssetManagementModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RRPNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.ReturnCategory).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReturnedByEmployeeId).IsRequired();
        builder.Property(x => x.ApprovedByEmployeeId).IsRequired();
        builder.Property(x => x.SignedByEmployeeId).IsRequired();
        builder.Property(x => x.PropertyInspectorCertified).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.RRPNo }).IsUnique();
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.ReturnCategory);
        builder.HasIndex(x => x.ReturnedByEmployeeId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
    }
}
