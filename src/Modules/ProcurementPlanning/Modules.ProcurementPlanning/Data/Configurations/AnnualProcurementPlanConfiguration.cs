using FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.ProcurementPlanning.Data.Configurations;

internal sealed class AnnualProcurementPlanConfiguration : IEntityTypeConfiguration<AnnualProcurementPlan>
{
    public void Configure(EntityTypeBuilder<AnnualProcurementPlan> builder)
    {
        builder.ToTable("AnnualProcurementPlans", ProcurementPlanningModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.AppNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AmendmentReason).HasMaxLength(1000);
        builder.Property(x => x.ReturnReason).HasMaxLength(1000);

        builder.HasIndex(x => x.AppNumber);
        builder.HasIndex(x => x.VersionChainId);
        builder.HasIndex(x => new { x.FiscalYear, x.IsCurrentVersion });

        builder.Navigation(x => x.SourcePpmps).HasField("_sourcePpmps");
        builder.HasMany(x => x.SourcePpmps)
            .WithOne()
            .HasForeignKey(x => x.AppId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.LineItems).HasField("_lineItems");
        builder.HasMany(x => x.LineItems)
            .WithOne()
            .HasForeignKey(x => x.AppId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AppSourcePpmpConfiguration : IEntityTypeConfiguration<AppSourcePpmp>
{
    public void Configure(EntityTypeBuilder<AppSourcePpmp> builder)
    {
        builder.ToTable("AppSourcePpmps", ProcurementPlanningModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PpmpNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OfficeCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EndUserUnit).HasMaxLength(256).IsRequired();

        builder.HasIndex(x => x.AppId);
        builder.HasIndex(x => x.PpmpId);
        builder.HasIndex(x => new { x.AppId, x.PpmpId }).IsUnique();
    }
}

internal sealed class AppLineItemConfiguration : IEntityTypeConfiguration<AppLineItem>
{
    public void Configure(EntityTypeBuilder<AppLineItem> builder)
    {
        builder.ToTable("AppLineItems", ProcurementPlanningModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourcePpmpNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OfficeCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EndUserUnit).HasMaxLength(256).IsRequired();
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

        builder.HasIndex(x => x.AppId);
        builder.HasIndex(x => x.SourcePpmpId);
        builder.HasIndex(x => x.SourcePpmpItemId);
        builder.HasIndex(x => new { x.AppId, x.SourcePpmpItemId }).IsUnique();
    }
}
