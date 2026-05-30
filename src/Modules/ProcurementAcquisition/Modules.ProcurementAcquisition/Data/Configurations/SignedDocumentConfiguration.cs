using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.ProcurementAcquisition.Domain.SignedDocuments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementAcquisition.Data.Configurations;

public sealed class SignedDocumentConfiguration : IEntityTypeConfiguration<SignedDocument>
{
    public void Configure(EntityTypeBuilder<SignedDocument> builder)
    {
        builder.ToTable("SignedDocuments", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DocumentType).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UploadedByName).HasMaxLength(200);

        // One current official copy per (tenant, document).
        builder.HasIndex(x => new { x.TenantId, x.DocumentType, x.DocumentId }).IsUnique();

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
    }
}
