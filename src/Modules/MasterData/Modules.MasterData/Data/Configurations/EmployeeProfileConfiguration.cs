using AMIS.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.MasterData.Data.Configurations;

public sealed class EmployeeProfileConfiguration : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeProfile> builder)
    {
        builder.ToTable("EmployeeProfiles", MasterDataModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmployeeNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.IdentityUserId).HasMaxLength(64);
        builder.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.WorkEmail).HasMaxLength(256);
        builder.Property(x => x.OfficeCode).HasMaxLength(8);
        // Optimistic concurrency via the Postgres system column xmin (mirrors AssetRegistry/Product) —
        // avoids the bytea IsRowVersion() pitfall and needs no domain-managed token field.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(x => x.Office)
            .WithMany()
            .HasForeignKey(x => x.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Position)
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EmployeeNumber).IsUnique();
        builder.HasIndex(x => x.IdentityUserId).IsUnique();
        builder.HasIndex(x => x.OfficeId);
        builder.HasIndex(x => x.DepartmentId);
        builder.HasIndex(x => x.PositionId);
        builder.HasIndex(x => x.OfficeCode);

        // NOTE (tenancy): MasterData reference tables — employees, suppliers, offices, departments,
        // positions, units of measure — are INTENTIONALLY shared across all tenants. They are deliberately
        // NOT .IsMultiTenant(); do not add it here. Because this entity is not multi-tenant, the anonymous
        // soft-delete filter form is also acceptable (the named form is only required alongside .IsMultiTenant()).
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
    }
}

