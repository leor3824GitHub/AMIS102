using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Counting;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class PhysicalCountSessionConfiguration : IEntityTypeConfiguration<PhysicalCountSession>
{
    public void Configure(EntityTypeBuilder<PhysicalCountSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PhysicalCountSessions", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.FundCluster).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Remarks).HasMaxLength(2000);

        builder.OwnsOne(x => x.ApprovedBy, n => n.ConfigureEmployeeRef("ApprovedBy"));
        builder.OwnsOne(x => x.WitnessedBy, n => n.ConfigureEmployeeRef("WitnessedBy"));

        // Multi-member ConductedBy collection — stored as a separate owned-collection table.
        builder.OwnsMany(x => x.ConductedBy, c =>
        {
            c.ToTable("PhysicalCountSessionConductors", AssetRegisterModuleConstants.SchemaName);
            c.WithOwner().HasForeignKey("PhysicalCountSessionId");
            c.Property<int>("Ordinal");
            c.HasKey("PhysicalCountSessionId", "Ordinal");
            c.Property(x => x.EmployeeId).HasColumnName("EmployeeId");
            c.Property(x => x.PrintedName).IsRequired().HasMaxLength(200).HasColumnName("PrintedName");
            c.Property(x => x.Designation).HasMaxLength(200).HasColumnName("Designation");
        });

        builder.HasMany(x => x.Entries)
            .WithOne()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Entries).AutoInclude(false);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

internal sealed class PhysicalCountEntryConfiguration : IEntityTypeConfiguration<PhysicalCountEntry>
{
    public void Configure(EntityTypeBuilder<PhysicalCountEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PhysicalCountEntries", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SnapshotArticle).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SnapshotUnit).IsRequired().HasMaxLength(64);
        builder.Property(x => x.SnapshotUnitCost).HasPrecision(18, 2);
        builder.Property(x => x.PhotoPath).HasMaxLength(1000);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.ProposedPropertyClass).HasMaxLength(64);
        builder.Property(x => x.ProposedCategoryCode).HasMaxLength(64);
        builder.Property(x => x.ProposedUnitCost).HasPrecision(18, 2);

        builder.OwnsOne(x => x.Snapshot, n => n.ConfigureSnapshot());

        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.AssetRegistryId);
    }
}

