using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.MasterData.Features.v1.Lookups;

public sealed class GetEmployeeReferenceByIdQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<GetEmployeeReferenceByIdQuery, EmployeeReferenceDto?>
{
    public async ValueTask<EmployeeReferenceDto?> Handle(GetEmployeeReferenceByIdQuery query, CancellationToken cancellationToken)
    {
        return await MasterDataLookupQueryBuilder.BuildEmployeeReferenceQuery(dbContext)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class GetEmployeeReferencesByIdsQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<GetEmployeeReferencesByIdsQuery, IReadOnlyDictionary<Guid, EmployeeReferenceDto>>
{
    public async ValueTask<IReadOnlyDictionary<Guid, EmployeeReferenceDto>> Handle(GetEmployeeReferencesByIdsQuery query, CancellationToken cancellationToken)
    {
        var employeeIds = query.Ids
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (employeeIds.Count == 0)
        {
            return new Dictionary<Guid, EmployeeReferenceDto>();
        }

        var employees = await MasterDataLookupQueryBuilder.BuildEmployeeReferenceQuery(
                dbContext,
                employeeFilter: e => employeeIds.Contains(e.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return employees.ToDictionary(x => x.Id);
    }
}

public sealed class GetEmployeeReferenceByIdentityUserIdQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<GetEmployeeReferenceByIdentityUserIdQuery, EmployeeReferenceDto?>
{
    public async ValueTask<EmployeeReferenceDto?> Handle(GetEmployeeReferenceByIdentityUserIdQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.IdentityUserId))
        {
            return null;
        }

        var identityUserId = query.IdentityUserId.Trim();

        return await MasterDataLookupQueryBuilder.BuildEmployeeReferenceQuery(
                dbContext, employeeFilter: e => e.IdentityUserId == identityUserId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class SearchEmployeeReferencesQueryHandler(MasterDataDbContext dbContext, ILogger<SearchEmployeeReferencesQueryHandler> logger)
    : IQueryHandler<SearchEmployeeReferencesQuery, PagedResponse<EmployeeReferenceDto>>
{
    public async ValueTask<PagedResponse<EmployeeReferenceDto>> Handle(SearchEmployeeReferencesQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var employeeQuery = MasterDataLookupQueryBuilder.BuildEmployeeReferenceQuery(
                dbContext,
                keyword: query.Keyword,
                identityUserId: query.IdentityUserId,
                officeId: query.OfficeId,
                departmentId: query.DepartmentId,
                positionId: query.PositionId,
                isActive: query.IsActive);

            return await employeeQuery.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SearchEmployeeReferencesQueryHandler: {ErrorMessage}. Query: Keyword={Keyword}, OfficeId={OfficeId}, DepartmentId={DepartmentId}, PositionId={PositionId}, IsActive={IsActive}, PageNumber={PageNumber}, PageSize={PageSize}",
                ex.Message, query.Keyword, query.OfficeId, query.DepartmentId, query.PositionId, query.IsActive, query.PageNumber, query.PageSize);
            throw;
        }
    }
}

public sealed class ListOfficeReferencesQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<ListOfficeReferencesQuery, PagedResponse<OfficeReferenceDto>>
{
    public async ValueTask<PagedResponse<OfficeReferenceDto>> Handle(ListOfficeReferencesQuery query, CancellationToken cancellationToken)
    {
        var officesQuery = dbContext.Offices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = $"%{query.Keyword}%";
            officesQuery = officesQuery.Where(x =>
                EF.Functions.ILike(x.Code, pattern) ||
                EF.Functions.ILike(x.Name, pattern) ||
                (x.LocationCode != null && EF.Functions.ILike(x.LocationCode, pattern)) ||
                (x.RegProvCode != null && EF.Functions.ILike(x.RegProvCode, pattern)) ||
                (x.Address != null && EF.Functions.ILike(x.Address, pattern)) ||
                (x.Description != null && EF.Functions.ILike(x.Description, pattern)));
        }

        if (query.IsActive.HasValue)
        {
            officesQuery = officesQuery.Where(x => x.IsActive == query.IsActive.Value);
        }

        officesQuery = officesQuery.OrderBy(x => x.Name).ThenBy(x => x.Code);

        return await officesQuery
            .Select(x => new OfficeReferenceDto(x.Id, x.Code, x.Name, x.Description, x.Address, x.LocationCode, x.RegProvCode, x.IsActive, x.OfficeCode))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class GetOfficeReferenceByIdQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<GetOfficeReferenceByIdQuery, OfficeReferenceDto?>
{
    public async ValueTask<OfficeReferenceDto?> Handle(GetOfficeReferenceByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.Offices.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new OfficeReferenceDto(x.Id, x.Code, x.Name, x.Description, x.Address, x.LocationCode, x.RegProvCode, x.IsActive, x.OfficeCode))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class ListDepartmentReferencesQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<ListDepartmentReferencesQuery, PagedResponse<DepartmentReferenceDto>>
{
    public async ValueTask<PagedResponse<DepartmentReferenceDto>> Handle(ListDepartmentReferencesQuery query, CancellationToken cancellationToken)
    {
        var departmentsQuery = dbContext.Departments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            departmentsQuery = departmentsQuery.Where(x =>
                x.Code.Contains(query.Keyword) ||
                x.Name.Contains(query.Keyword) ||
                (x.Description != null && x.Description.Contains(query.Keyword)));
        }

        if (query.IsActive.HasValue)
        {
            departmentsQuery = departmentsQuery.Where(x => x.IsActive == query.IsActive.Value);
        }

        departmentsQuery = departmentsQuery.OrderBy(x => x.Name).ThenBy(x => x.Code);

        return await departmentsQuery
            .Select(x => new DepartmentReferenceDto(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.OfficeCode))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class GetDepartmentReferenceByIdQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<GetDepartmentReferenceByIdQuery, DepartmentReferenceDto?>
{
    public async ValueTask<DepartmentReferenceDto?> Handle(GetDepartmentReferenceByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.Departments.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new DepartmentReferenceDto(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.OfficeCode))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class ListPositionReferencesQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<ListPositionReferencesQuery, PagedResponse<PositionReferenceDto>>
{
    public async ValueTask<PagedResponse<PositionReferenceDto>> Handle(ListPositionReferencesQuery query, CancellationToken cancellationToken)
    {
        var positionsQuery = dbContext.Positions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            positionsQuery = positionsQuery.Where(x =>
                x.Code.Contains(query.Keyword) ||
                x.Name.Contains(query.Keyword) ||
                (x.Description != null && x.Description.Contains(query.Keyword)));
        }

        if (query.IsActive.HasValue)
        {
            positionsQuery = positionsQuery.Where(x => x.IsActive == query.IsActive.Value);
        }

        positionsQuery = positionsQuery.OrderBy(x => x.Name).ThenBy(x => x.Code);

        return await positionsQuery
            .Select(x => new PositionReferenceDto(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.OfficeCode))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class GetPositionReferenceByIdQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<GetPositionReferenceByIdQuery, PositionReferenceDto?>
{
    public async ValueTask<PositionReferenceDto?> Handle(GetPositionReferenceByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.Positions.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new PositionReferenceDto(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.OfficeCode))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class ListUnitOfMeasureReferencesQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<ListUnitOfMeasureReferencesQuery, PagedResponse<UnitOfMeasureReferenceDto>>
{
    public async ValueTask<PagedResponse<UnitOfMeasureReferenceDto>> Handle(ListUnitOfMeasureReferencesQuery query, CancellationToken cancellationToken)
    {
        var uomQuery = dbContext.UnitOfMeasures.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            uomQuery = uomQuery.Where(x =>
                x.Code.Contains(query.Keyword) ||
                x.Name.Contains(query.Keyword) ||
                (x.Description != null && x.Description.Contains(query.Keyword)));
        }

        if (query.IsActive.HasValue)
        {
            uomQuery = uomQuery.Where(x => x.IsActive == query.IsActive.Value);
        }

        uomQuery = uomQuery.OrderBy(x => x.Name).ThenBy(x => x.Code);

        return await uomQuery
            .Select(x => new UnitOfMeasureReferenceDto(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.OfficeCode))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class GetUnitOfMeasureReferenceByIdQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<GetUnitOfMeasureReferenceByIdQuery, UnitOfMeasureReferenceDto?>
{
    public async ValueTask<UnitOfMeasureReferenceDto?> Handle(GetUnitOfMeasureReferenceByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.UnitOfMeasures.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new UnitOfMeasureReferenceDto(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.OfficeCode))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

internal static class MasterDataLookupQueryBuilder
{
    internal static IQueryable<EmployeeReferenceDto> BuildEmployeeReferenceQuery(
        MasterDataDbContext dbContext,
        System.Linq.Expressions.Expression<Func<AMIS.Modules.MasterData.Domain.EmployeeProfile, bool>>? employeeFilter = null,
        string? keyword = null,
        string? identityUserId = null,
        Guid? officeId = null,
        Guid? departmentId = null,
        Guid? positionId = null,
        bool? isActive = null)
    {
        var employees = dbContext.Employees.AsNoTracking();

        if (employeeFilter is not null)
            employees = employees.Where(employeeFilter);

        if (!string.IsNullOrWhiteSpace(identityUserId))
            employees = employees.Where(e => e.IdentityUserId == identityUserId);

        if (officeId.HasValue)
            employees = employees.Where(e => e.OfficeId == officeId.Value);

        if (departmentId.HasValue)
            employees = employees.Where(e => e.DepartmentId == departmentId.Value);

        if (positionId.HasValue)
            employees = employees.Where(e => e.PositionId == positionId.Value);

        if (isActive.HasValue)
            employees = employees.Where(e => e.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
            employees = employees.Where(e =>
                e.EmployeeNumber.Contains(keyword) ||
                e.FirstName.Contains(keyword) ||
                e.LastName.Contains(keyword) ||
                (e.WorkEmail != null && e.WorkEmail.Contains(keyword)) ||
                (e.Office != null && (e.Office.Code.Contains(keyword) || e.Office.Name.Contains(keyword))) ||
                (e.Department != null && (e.Department.Code.Contains(keyword) || e.Department.Name.Contains(keyword))) ||
                (e.Position != null && (e.Position.Code.Contains(keyword) || e.Position.Name.Contains(keyword))));

        return employees
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ThenBy(employee => employee.EmployeeNumber)
            .Select(employee => new EmployeeReferenceDto(
                employee.Id,
                employee.EmployeeNumber ?? string.Empty,
                employee.IdentityUserId,
                employee.FirstName ?? string.Empty,
                employee.LastName ?? string.Empty,
                employee.WorkEmail,
                employee.OfficeId,
                employee.Office != null ? employee.Office.Code : string.Empty,
                employee.Office != null ? employee.Office.Name : string.Empty,
                employee.DepartmentId,
                employee.Department != null ? employee.Department.Code : string.Empty,
                employee.Department != null ? employee.Department.Name : string.Empty,
                employee.PositionId,
                employee.Position != null ? employee.Position.Code : string.Empty,
                employee.Position != null ? employee.Position.Name : string.Empty,
                employee.DefaultUnitOfMeasureId,
                employee.DefaultUnitOfMeasure != null ? employee.DefaultUnitOfMeasure.Code : null,
                employee.DefaultUnitOfMeasure != null ? employee.DefaultUnitOfMeasure.Name : null,
                employee.IsActive,
                employee.OfficeCode));
    }
}




