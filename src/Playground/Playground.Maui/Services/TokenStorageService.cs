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
