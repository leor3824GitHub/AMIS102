using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.AssetRegister.Domain.Issuance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class PPEIRFormSeriesConfiguration : IEntityTypeConfiguration<PPEIRFormSeries>
{
    public void Configure(EntityTypeBuilder<PPEIRFormSeries> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PPEIRFormSeries", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Label).IsRequired().HasMaxLength(200);
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Ignore(x => x.IsExhausted);
        builder.Ignore(x => x.Remaining);

        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}
