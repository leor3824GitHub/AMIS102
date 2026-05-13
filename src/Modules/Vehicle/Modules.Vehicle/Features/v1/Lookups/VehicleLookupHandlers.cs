using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Vehicle.Contracts.v1.References;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Vehicle.Features.v1.Lookups;

public sealed class GetVehicleReferenceByIdQueryHandler(VehicleDbContext dbContext)
    : IQueryHandler<GetVehicleReferenceByIdQuery, VehicleReferenceDto?>
{
    public async ValueTask<VehicleReferenceDto?> Handle(GetVehicleReferenceByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.Vehicles.AsNoTracking()
            .Where(v => v.Id == query.Id)
            .Select(v => new VehicleReferenceDto(
                v.Id,
                v.PlateNumber,
                v.Make,
                v.Model,
                v.Year,
                v.Type.ToString(),
                v.Status.ToString(),
                v.Odometer,
                v.AssignedDepartmentId,
                v.AssignedDepartment,
                v.AssignedDriverId,
                v.AssignedDriver))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class SearchVehicleReferencesQueryHandler(VehicleDbContext dbContext, ILogger<SearchVehicleReferencesQueryHandler> logger)
    : IQueryHandler<SearchVehicleReferencesQuery, PagedResponse<VehicleReferenceDto>>
{
    public async ValueTask<PagedResponse<VehicleReferenceDto>> Handle(SearchVehicleReferencesQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var vehicleQuery = dbContext.Vehicles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                vehicleQuery = vehicleQuery.Where(v =>
                    v.PlateNumber.Contains(query.Keyword) ||
                    v.Make.Contains(query.Keyword) ||
                    v.Model.Contains(query.Keyword) ||
                    (v.AssignedDepartment != null && v.AssignedDepartment.Contains(query.Keyword)) ||
                    (v.AssignedDriver != null && v.AssignedDriver.Contains(query.Keyword)));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<VehicleStatus>(query.Status, ignoreCase: true, out var status))
            {
                vehicleQuery = vehicleQuery.Where(v => v.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Type) &&
                Enum.TryParse<VehicleType>(query.Type, ignoreCase: true, out var type))
            {
                vehicleQuery = vehicleQuery.Where(v => v.Type == type);
            }

            if (query.AssignedDepartmentId.HasValue)
            {
                vehicleQuery = vehicleQuery.Where(v => v.AssignedDepartmentId == query.AssignedDepartmentId.Value);
            }

            vehicleQuery = vehicleQuery
                .OrderBy(v => v.PlateNumber)
                .ThenBy(v => v.Make)
                .ThenBy(v => v.Model);

            return await vehicleQuery
                .Select(v => new VehicleReferenceDto(
                    v.Id,
                    v.PlateNumber,
                    v.Make,
                    v.Model,
                    v.Year,
                    v.Type.ToString(),
                    v.Status.ToString(),
                    v.Odometer,
                    v.AssignedDepartmentId,
                    v.AssignedDepartment,
                    v.AssignedDriverId,
                    v.AssignedDriver))
                .ToPagedResponseAsync(query, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SearchVehicleReferencesQueryHandler: {ErrorMessage}. Query: Keyword={Keyword}, Status={Status}, Type={Type}, PageNumber={PageNumber}, PageSize={PageSize}",
                ex.Message, query.Keyword, query.Status, query.Type, query.PageNumber, query.PageSize);
            throw;
        }
    }
}

