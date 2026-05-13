using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.ProcurementAcquisition.Domain.Canvass;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementAcquisition.Data.Configurations;

public sealed class CanvassQuotationConfiguration : IEntityTypeConfiguration<CanvassQuotation>
{
    public void Configure(EntityTypeBuilder<CanvassQuotation> builder)
    {
        builder.ToTable("CanvassQuotations", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SupplierName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SupplierAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TinNumber).HasMaxLength(32);
        builder.Property(x => x.DeliveryTerms).HasMaxLength(256);

        builder.HasIndex(x => x.CanvassRequestId);
        builder.HasIndex(x => x.SupplierId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        builder.OwnsMany(x => x.LineItems, b =>
        {
            b.ToJson();
            b.Property(li => li.ItemNo).IsRequired();
            b.Property(li => li.Description).HasMaxLength(500).IsRequired();
            b.Property(li => li.Unit).HasMaxLength(64).IsRequired();
            b.Property(li => li.Quantity).HasPrecision(18, 4).IsRequired();
            b.Property(li => li.UnitPrice).HasPrecision(18, 4).IsRequired();
        });
    }
}

