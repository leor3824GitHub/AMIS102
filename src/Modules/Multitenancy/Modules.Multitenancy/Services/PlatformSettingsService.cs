using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Caching;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Multitenancy.Contracts.Dtos;
using AMIS.Modules.Multitenancy.Data;
using AMIS.Modules.Multitenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Multitenancy.Services;

public sealed class PlatformSettingsService : IPlatformSettingsService
{
    private const string CacheKey = "platform-settings:global";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly ICacheService _cache;
    private readonly TenantDbContext _dbContext;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PlatformSettingsService> _logger;

    public PlatformSettingsService(
        ICacheService cache,
        TenantDbContext dbContext,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        ICurrentUser currentUser,
        ILogger<PlatformSettingsService> logger)
    {
        _cache = cache;
        _dbContext = dbContext;
        _tenantAccessor = tenantAccessor;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PlatformSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await _cache.GetOrSetAsync(
            CacheKey,
            async () => await LoadFromDbAsync(ct).ConfigureAwait(false),
            CacheDuration,
            ct).ConfigureAwait(false);

        return settings ?? PlatformSettingsDto.Default;
    }

    public async Task UpdateAsync(PlatformSettingsDto settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Global settings may only be changed from the root tenant.
        var currentTenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (!string.Equals(currentTenantId, MultitenancyConstants.Root.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Only the root tenant can change global platform settings.");
        }

        var entity = await _dbContext.PlatformSettings
            .FirstOrDefaultAsync(s => s.Id == PlatformSettings.SingletonId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = PlatformSettings.CreateDefault(GetCurrentUserId());
            _dbContext.PlatformSettings.Add(entity);
        }

        MapDtoToEntity(settings, entity);
        entity.Update(GetCurrentUserId());

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        await _cache.RemoveItemAsync(CacheKey, ct).ConfigureAwait(false);

        _logger.LogInformation("Updated global platform settings.");
    }

    private async Task<PlatformSettingsDto?> LoadFromDbAsync(CancellationToken ct)
    {
        var entity = await _dbContext.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == PlatformSettings.SingletonId, ct)
            .ConfigureAwait(false);

        return entity is null ? null : MapEntityToDto(entity);
    }

    private static PlatformSettingsDto MapEntityToDto(PlatformSettings entity) => new()
    {
        Session = new SessionSettingsDto
        {
            MaxSessionsPerUser = entity.MaxSessionsPerUser,
            IdleTimeoutMinutes = entity.IdleTimeoutMinutes,
            AbsoluteTimeoutDays = entity.AbsoluteTimeoutDays
        },
        Quota = new QuotaSettingsDto
        {
            MaxUsersPerTenant = entity.MaxUsersPerTenant,
            StorageLimitMb = entity.StorageLimitMb,
            ApiRateLimitPerMinute = entity.ApiRateLimitPerMinute
        }
    };

    private static void MapDtoToEntity(PlatformSettingsDto dto, PlatformSettings entity)
    {
        entity.MaxSessionsPerUser = dto.Session.MaxSessionsPerUser;
        entity.IdleTimeoutMinutes = dto.Session.IdleTimeoutMinutes;
        entity.AbsoluteTimeoutDays = dto.Session.AbsoluteTimeoutDays;

        entity.MaxUsersPerTenant = dto.Quota.MaxUsersPerTenant;
        entity.StorageLimitMb = dto.Quota.StorageLimitMb;
        entity.ApiRateLimitPerMinute = dto.Quota.ApiRateLimitPerMinute;
    }

    private string? GetCurrentUserId()
    {
        var userId = _currentUser.GetUserId();
        return userId == Guid.Empty ? null : userId.ToString();
    }
}
