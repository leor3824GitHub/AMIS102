using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.MasterData.Data.Configurations;

public sealed class OrganizationProfileConfiguration : IEntityTypeConfiguration<OrganizationProfile>
{
    public void Configure(EntityTypeBuilder<OrganizationProfile> builder)
    {
        builder.ToTable("OrganizationProfiles", MasterDataModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShortName).HasMaxLength(80);
        builder.Property(x => x.Address).HasMaxLength(400);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.AnnexECode).HasMaxLength(8);
        builder.Property(x => x.RegionalManagerId);
        builder.Property(x => x.RegionalManagerName).HasMaxLength(200);
        builder.Property(x => x.RegionalManagerDesignation).HasMaxLength(200);
        builder.Property(x => x.AssistantRegionalManagerId);
        builder.Property(x => x.AssistantRegionalManagerName).HasMaxLength(200);
        builder.Property(x => x.AssistantRegionalManagerDesignation).HasMaxLength(200);
        builder.Property(x => x.AccountantId);
        builder.Property(x => x.AccountantName).HasMaxLength(200);
        builder.Property(x => x.AccountantDesignation).HasMaxLength(200);
        builder.Property(x => x.SupervisingAdminOfficerId);
        builder.Property(x => x.SupervisingAdminOfficerName).HasMaxLength(200);
        builder.Property(x => x.SupervisingAdminOfficerDesignation).HasMaxLength(200);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}

