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
        builder.Property(x => x.AmendedById).HasMaxLength(256);
        builder.Property(x => x.ConsolidatedById).HasMaxLength(256);
        builder.Property(x => x.ApprovedById).HasMaxLength(256);
        builder.Property(x => x.ReturnReason).HasMaxLength(1000);

        builder.Property(x => x.Version).IsRowVersion();

        builder.HasIndex(x => x.AppNumber);
        builder.HasIndex(x => x.VersionChainId);
        builder.HasIndex(x => new { x.FiscalYear, x.IsCurrentVersion });

        builder.HasMany(x => x.Items)
               .WithOne()
               .HasForeignKey(x => x.AppId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AppItemConfiguration : IEntityTypeConfiguration<AppItem>
{
    public void Configure(EntityTypeBuilder<AppItem> builder)
    {
        builder.ToTable("AppItems", ProcurementPlanningModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.OfficeCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EndUserUnit).HasMaxLength(256).IsRequired();
        builder.Property(x => x.GeneralDescription).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProcurementStart).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ProcurementEnd).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ExpectedDelivery).HasMaxLength(10).IsRequired();
        builder.Property(x => x.SourceOfFunds).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EstimatedBudget).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => x.AppId);
        builder.HasIndex(x => x.SourcePpmpId);
    }
}
