namespace Playground.Maui.Services;

public sealed class TokenStorageService : ITokenStorageService
{
    private const string AccessTokenKey = "amis_access_token";
    private const string RefreshTokenKey = "amis_refresh_token";

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
#if WINDOWS
    SaveToEncryptedFile(AccessTokenKey, accessToken);
    SaveToEncryptedFile(RefreshTokenKey, refreshToken);
        await Task.CompletedTask;
#else
        await SecureStorage.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
#endif
    }

    public async Task<string?> GetAccessTokenAsync()
    {
#if WINDOWS
        return await Task.FromResult(GetFromEncryptedFile(AccessTokenKey));
#else
        return await SecureStorage.GetAsync(AccessTokenKey);
#endif
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
#if WINDOWS
        return await Task.FromResult(GetFromEncryptedFile(RefreshTokenKey));
#else
        return await SecureStorage.GetAsync(RefreshTokenKey);
#endif
    }

    public async Task ClearAsync()
    {
#if WINDOWS
        ClearEncryptedFile(AccessTokenKey);
        ClearEncryptedFile(RefreshTokenKey);
        await Task.CompletedTask;
#else
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
        await Task.CompletedTask;
#endif
    }

    public Task SaveLastSessionAsync(string userId, string tenantId)
    {
        Preferences.Set("amis_last_user_id", userId);
        Preferences.Set("amis_last_tenant_id", tenantId);
        return Task.CompletedTask;
    }

    public Task<(string? UserId, string? TenantId)> GetLastSessionAsync()
    {
        var userId = Preferences.Get("amis_last_user_id", (string?)null);
        var tenantId = Preferences.Get("amis_last_tenant_id", (string?)null);
        return Task.FromResult((userId, tenantId));
    }

#if WINDOWS
    private static string GetTokenFilePath(string key) =>
        Path.Combine(FileSystem.AppDataDirectory, $"{key}.dat");

    private static void SaveToEncryptedFile(string key, string value)
    {
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(value);
        var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(
            plainBytes,
            optionalEntropy: null,
            scope: System.Security.Cryptography.DataProtectionScope.CurrentUser);

        File.WriteAllBytes(GetTokenFilePath(key), encryptedBytes);
    }

    private static string? GetFromEncryptedFile(string key)
    {
        try
        {
            var path = GetTokenFilePath(key);
            if (!File.Exists(path))
                return null;

            var encryptedBytes = File.ReadAllBytes(path);
            var plainBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                encryptedBytes,
                optionalEntropy: null,
                scope: System.Security.Cryptography.DataProtectionScope.CurrentUser);

            return System.Text.Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return null;
        }
    }

    private static void ClearEncryptedFile(string key)
    {
        try
        {
            var path = GetTokenFilePath(key);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }
#endif
}
