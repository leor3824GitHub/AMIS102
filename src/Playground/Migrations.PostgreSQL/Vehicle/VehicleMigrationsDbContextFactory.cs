using FSH.Modules.Vehicle.Data;
using Microsoft.EntityFrameworkCore.Design;

namespace FSH.Playground.Migrations.PostgreSQL.Vehicle;

public sealed class VehicleMigrationsDbContextFactory : IDesignTimeDbContextFactory<VehicleDbContext>
{
    public VehicleDbContext CreateDbContext(string[] args) =>
        new VehicleDbContextFactory().CreateDbContext(args);
}
