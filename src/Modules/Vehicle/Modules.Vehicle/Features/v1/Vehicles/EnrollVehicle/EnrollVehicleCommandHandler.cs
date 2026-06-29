using System.Net;
using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Vehicles;
using AMIS.Modules.Vehicle.Features.v1.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;
using VehicleEntity = AMIS.Modules.Vehicle.Domain.Vehicles.Vehicle;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.EnrollVehicle;

public sealed class EnrollVehicleCommandHandler(VehicleDbContext db, IMediator mediator, ICurrentUser currentUser)
    : ICommandHandler<EnrollVehicleCommand, VehicleDto>
{
    public async ValueTask<VehicleDto> Handle(EnrollVehicleCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required");

        // Source of truth for the asset lives in AssetRegister — fetch via Contracts (Mediator), never a FK.
        var asset = await mediator.Send(new GetAssetRegistryQuery(cmd.AssetRegistryId), cancellationToken).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.AssetRegistryId), "PPE asset not found.")]);

        if (asset.AssetType != AssetType.PPE)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.AssetRegistryId), "Selected asset is not a PPE asset.")]);

        if (!string.Equals(asset.PropertyClass, AssetClassCodes.Vehicle, StringComparison.OrdinalIgnoreCase))
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.AssetRegistryId), "Selected asset is not a motor vehicle (property class LT).")]);

        if (asset.LifecycleState is LifecycleState.Disposed or LifecycleState.TransferredOut)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.AssetRegistryId), "This asset has been disposed or transferred out and cannot be enrolled.")]);

        var alreadyEnrolled = await db.Vehicles
            .IgnoreQueryFilters()
            .AnyAsync(v => v.TenantId == tenantId && v.AssetRegistryId == cmd.AssetRegistryId, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyEnrolled)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.AssetRegistryId), "This asset is already enrolled as a vehicle.")]);

        var plateExists = await db.Vehicles
            .IgnoreQueryFilters()
            .AnyAsync(v => v.TenantId == tenantId && v.PlateNumber == cmd.PlateNumber.ToUpperInvariant(), cancellationToken)
            .ConfigureAwait(false);
        if (plateExists)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.PlateNumber), "A vehicle with this plate number already exists.")]);

        Enum.TryParse<VehicleType>(cmd.Type, ignoreCase: true, out var vehicleType);

        var vehicle = VehicleEntity.Enroll(tenantId, asset.Id, asset.PropertyNo,
            asset.UnitCost, asset.AcquisitionDate,
            cmd.PlateNumber, cmd.Make, cmd.Model, cmd.Year, vehicleType,
            cmd.Odometer, cmd.Notes, cmd.MotorNumber, cmd.ChassisNumber,
            cmd.NumberOfCylinders, cmd.EngineDisplacementCC, cmd.FuelType, cmd.VehicleUse);
        vehicle.SetCreatedBy(currentUser.GetUserId().ToString());

        db.Vehicles.Add(vehicle);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // Race fallback: another request enrolled this asset / plate between the check and save.
            throw new CustomException("This asset is already enrolled as a vehicle.", Array.Empty<string>(), HttpStatusCode.Conflict);
        }

        return vehicle.ToDto();
    }
}
