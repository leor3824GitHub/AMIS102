namespace Playground.Maui.Services;

public sealed class TokenStorageService : ITokenStorageService
{
    private const string AccessTokenKey = "amis_access_token";
    private const string RefreshTokenKey = "amis_refresh_token";
    private const string VaultResource = "amis_mobile";

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
#if WINDOWS
        SaveToVault(AccessTokenKey, accessToken);
        SaveToVault(RefreshTokenKey, refreshToken);
        await Task.CompletedTask;
#else
        await SecureStorage.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
#endif
    }

    public async Task<string?> GetAccessTokenAsync()
    {
#if WINDOWS
        return await Task.FromResult(GetFromVault(AccessTokenKey));
#else
        return await SecureStorage.GetAsync(AccessTokenKey);
#endif
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
#if WINDOWS
        return await Task.FromResult(GetFromVault(RefreshTokenKey));
#else
        return await SecureStorage.GetAsync(RefreshTokenKey);
#endif
    }

    public async Task ClearAsync()
    {
#if WINDOWS
        ClearFromVault(AccessTokenKey);
        ClearFromVault(RefreshTokenKey);
        await Task.CompletedTask;
#else
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
        await Task.CompletedTask;
#endif
    }

#if WINDOWS
    private static void SaveToVault(string key, string value)
    {
        var vault = new Windows.Security.Credentials.PasswordVault();
        try { vault.Remove(vault.Retrieve(VaultResource, key)); } catch { }
        vault.Add(new Windows.Security.Credentials.PasswordCredential(VaultResource, key, value));
    }

    private static string? GetFromVault(string key)
    {
        try
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            var cred = vault.Retrieve(VaultResource, key);
            cred.RetrievePassword();
            return cred.Password;
        }
        catch
        {
            return null;
        }
    }

    private static void ClearFromVault(string key)
    {
        try
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            vault.Remove(vault.Retrieve(VaultResource, key));
        }
        catch { }
    }
#endif
}
