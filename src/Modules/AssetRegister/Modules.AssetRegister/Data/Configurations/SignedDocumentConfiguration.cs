using AMIS.Modules.AssetRegister.Domain.SignedDocuments;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class SignedDocumentConfiguration : IEntityTypeConfiguration<SignedDocument>
{
    public void Configure(EntityTypeBuilder<SignedDocument> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SignedDocuments", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DocumentType).HasConversion<int>();
        builder.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UploadedByName).HasMaxLength(200);

        // One current official copy per (tenant, document).
        builder.HasIndex(x => new { x.TenantId, x.DocumentType, x.DocumentId }).IsUnique();
    }
}
