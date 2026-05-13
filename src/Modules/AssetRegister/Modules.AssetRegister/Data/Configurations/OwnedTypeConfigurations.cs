using FSH.Modules.AssetRegister.Contracts.v1.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetRegister.Data.Configurations;

internal static class OwnedTypeConfigurations
{
    public static OwnedNavigationBuilder<TOwner, AssetSnapshot> ConfigureSnapshot<TOwner>(
        this OwnedNavigationBuilder<TOwner, AssetSnapshot> builder,
        string columnPrefix = "Snapshot")
        where TOwner : class
    {
        builder.Property(x => x.PropertyNo).IsRequired().HasMaxLength(32).HasColumnName($"{columnPrefix}_PropertyNo");
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500).HasColumnName($"{columnPrefix}_Description");
        builder.Property(x => x.AssetType).HasColumnName($"{columnPrefix}_AssetType");
        builder.Property(x => x.UnitCost).HasPrecision(18, 2).HasColumnName($"{columnPrefix}_UnitCost");
        builder.Property(x => x.Unit).IsRequired().HasMaxLength(64).HasColumnName($"{columnPrefix}_Unit");
        builder.Property(x => x.EstimatedUsefulLifeYears).HasColumnName($"{columnPrefix}_EstimatedUsefulLifeYears");
        builder.Property(x => x.AcquisitionDate).HasColumnName($"{columnPrefix}_AcquisitionDate");
        builder.Property(x => x.UacsObjectCode).HasMaxLength(32).HasColumnName($"{columnPrefix}_UacsObjectCode");
        builder.Property(x => x.SerialNo).HasMaxLength(200).HasColumnName($"{columnPrefix}_SerialNo");
        builder.Property(x => x.Brand).HasMaxLength(200).HasColumnName($"{columnPrefix}_Brand");
        builder.Property(x => x.Model).HasMaxLength(200).HasColumnName($"{columnPrefix}_Model");
        return builder;
    }

    public static OwnedNavigationBuilder<TOwner, EmployeeRef> ConfigureEmployeeRef<TOwner>(
        this OwnedNavigationBuilder<TOwner, EmployeeRef> builder,
        string columnPrefix)
        where TOwner : class
    {
        builder.Property(x => x.EmployeeId).HasColumnName($"{columnPrefix}_EmployeeId");
        builder.Property(x => x.PrintedName).IsRequired().HasMaxLength(200).HasColumnName($"{columnPrefix}_PrintedName");
        builder.Property(x => x.Designation).HasMaxLength(200).HasColumnName($"{columnPrefix}_Designation");
        return builder;
    }
}
