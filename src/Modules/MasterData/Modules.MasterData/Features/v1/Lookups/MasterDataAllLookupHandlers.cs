using AMIS.Modules.MasterData.Contracts.v1.Categories;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Contracts.v1.Suppliers;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AMIS.Modules.MasterData.Features.v1.Lookups;

// Departments/Offices/Positions/UnitOfMeasures/Categories/Suppliers are NOT .IsMultiTenant()
// reference data (shared across tenants), so the cache keys are global — no cross-tenant leakage risk.
internal static class LookupCache
{
    internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(120);
}

public sealed class ListAllDepartmentsQueryHandler(MasterDataDbContext dbContext, IMemoryCache cache)
    : IQueryHandler<ListAllDepartmentsQuery, IReadOnlyList<DepartmentReferenceDto>>
{
    public async ValueTask<IReadOnlyList<DepartmentReferenceDto>> Handle(ListAllDepartmentsQuery query, CancellationToken cancellationToken)
        => await cache.GetOrCreateAsync("lookup:departments:all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LookupCache.Ttl;
            return (IReadOnlyList<DepartmentReferenceDto>)await dbContext.Departments.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name).ThenBy(x => x.Code)
                .Select(x => new DepartmentReferenceDto(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.OfficeCode, x.ResponsibilityCenterCode))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }).ConfigureAwait(false) ?? [];
}

public sealed class ListAllOfficesQueryHandler(MasterDataDbContext dbContext, IMemoryCache cache)
    : IQueryHandler<ListAllOfficesQuery, IReadOnlyList<OfficeReferenceDto>>
{
    public async ValueTask<IReadOnlyList<OfficeReferenceDto>> Handle(ListAllOfficesQuery query, CancellationToken cancellationToken)
        => await cache.GetOrCreateAsync("lookup:offices:all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LookupCache.Ttl;
            return (IReadOnlyList<OfficeReferenceDto>)await dbContext.Offices.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name).ThenBy(x => x.Code)
                .Select(x => new OfficeReferenceDto(x.Id, x.Code, x.Name, x.Description, x.Address, x.LocationCode, x.RegProvCode, x.IsActive, x.OfficeCode))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }).ConfigureAwait(false) ?? [];
}

public sealed class ListAllPositionsQueryHandler(MasterDataDbContext dbContext, IMemoryCache cache)
    : IQueryHandler<ListAllPositionsQuery, IReadOnlyList<PositionReferenceDto>>
{
    public async ValueTask<IReadOnlyList<PositionReferenceDto>> Handle(ListAllPositionsQuery query, CancellationToken cancellationToken)
        => await cache.GetOrCreateAsync("lookup:positions:all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LookupCache.Ttl;
            return (IReadOnlyList<PositionReferenceDto>)await dbContext.Positions.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name).ThenBy(x => x.Code)
                .Select(x => new PositionReferenceDto(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.OfficeCode))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }).ConfigureAwait(false) ?? [];
}

public sealed class ListAllUnitOfMeasuresQueryHandler(MasterDataDbContext dbContext, IMemoryCache cache)
    : IQueryHandler<ListAllUnitOfMeasuresQuery, IReadOnlyList<UnitOfMeasureReferenceDto>>
{
    public async ValueTask<IReadOnlyList<UnitOfMeasureReferenceDto>> Handle(ListAllUnitOfMeasuresQuery query, CancellationToken cancellationToken)
        => await cache.GetOrCreateAsync("lookup:unit-of-measures:all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LookupCache.Ttl;
            return (IReadOnlyList<UnitOfMeasureReferenceDto>)await dbContext.UnitOfMeasures.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name).ThenBy(x => x.Code)
                .Select(x => new UnitOfMeasureReferenceDto(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.OfficeCode))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }).ConfigureAwait(false) ?? [];
}

public sealed class ListAllCategoriesQueryHandler(MasterDataDbContext dbContext, IMemoryCache cache)
    : IQueryHandler<ListAllCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async ValueTask<IReadOnlyList<CategoryDto>> Handle(ListAllCategoriesQuery query, CancellationToken cancellationToken)
        => await cache.GetOrCreateAsync("lookup:categories:all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LookupCache.Ttl;
            return (IReadOnlyList<CategoryDto>)await dbContext.Categories.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name).ThenBy(x => x.Code)
                .Select(x => new CategoryDto(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.OfficeCode))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }).ConfigureAwait(false) ?? [];
}

public sealed class ListAllSuppliersQueryHandler(MasterDataDbContext dbContext, IMemoryCache cache)
    : IQueryHandler<ListAllSuppliersQuery, IReadOnlyList<SupplierDto>>
{
    public async ValueTask<IReadOnlyList<SupplierDto>> Handle(ListAllSuppliersQuery query, CancellationToken cancellationToken)
        => await cache.GetOrCreateAsync("lookup:suppliers:all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LookupCache.Ttl;
            return (IReadOnlyList<SupplierDto>)await dbContext.Suppliers.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name).ThenBy(x => x.Code)
                .Select(x => new SupplierDto(x.Id, x.Code, x.Name, x.TinNo, x.BusinessTaxType, x.Description,
                    x.ContactPerson, x.Email, x.Phone, x.Address, x.IsActive, x.OfficeCode))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }).ConfigureAwait(false) ?? [];
}
