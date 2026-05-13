using AMIS.Modules.ProcurementPlanning.Domain.Ppmps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementPlanning.Data.Configurations;

internal sealed class PpmpConfiguration : IEntityTypeConfiguration<Ppmp>
{
    public void Configure(EntityTypeBuilder<Ppmp> builder)
    {
        builder.ToTable("Ppmps", ProcurementPlanningModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PpmpNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OfficeCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EndUserUnit).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AmendmentReason).HasMaxLength(1000);
        builder.Property(x => x.ReturnReason).HasMaxLength(1000);

        builder.HasIndex(x => x.PpmpNumber);
        builder.HasIndex(x => x.VersionChainId);
        builder.HasIndex(x => new { x.FiscalYear, x.OfficeCode, x.IsCurrentVersion });

        builder.Navigation(x => x.Items).HasField("_items");
        builder.HasMany(x => x.Items)
               .WithOne()
               .HasForeignKey(x => x.PpmpId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PpmpItemConfiguration : IEntityTypeConfiguration<PpmpItem>
{
    public void Configure(EntityTypeBuilder<PpmpItem> builder)
    {
        builder.ToTable("PpmpItems", ProcurementPlanningModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.GeneralDescription).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ModeOfProcurement).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProcurementStart).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ProcurementEnd).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ExpectedDelivery).HasMaxLength(10).IsRequired();
        builder.Property(x => x.SourceOfFunds).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EstimatedBudget).HasColumnType("numeric(18,2)");
        builder.Property(x => x.SupportingDocuments).HasMaxLength(500);
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => x.PpmpId);
    }
}

