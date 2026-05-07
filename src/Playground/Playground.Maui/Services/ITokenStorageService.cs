namespace Playground.Maui.Services;

public interface ITokenStorageService
{
    Task SaveTokensAsync(string accessToken, string refreshToken);
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task ClearAsync();
    Task SaveLastSessionAsync(string userId, string tenantId);
    Task<(string? UserId, string? TenantId)> GetLastSessionAsync();
}
