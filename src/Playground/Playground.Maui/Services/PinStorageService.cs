using System.Security.Cryptography;
using System.Text;

namespace Playground.Maui.Services;

public sealed class PinStorageService : IPinStorageService
{
    private const string PinHashKey = "amis_pin_hash";

    public async Task<bool> IsPinSetAsync()
    {
        var hash = await GetPinHashAsync();
        return !string.IsNullOrEmpty(hash);
    }

    public async Task SavePinAsync(string pin, string userId)
    {
        var hash = ComputeHash(pin, userId);
#if WINDOWS
        SaveToEncryptedFile(PinHashKey, hash);
        await Task.CompletedTask;
#else
        await SecureStorage.SetAsync(PinHashKey, hash);
#endif
    }

    public async Task<bool> VerifyPinAsync(string pin, string userId)
    {
        var stored = await GetPinHashAsync();
        if (string.IsNullOrEmpty(stored)) return false;
        return stored == ComputeHash(pin, userId);
    }

    public async Task ClearAsync()
    {
#if WINDOWS
        ClearEncryptedFile(PinHashKey);
        await Task.CompletedTask;
#else
        SecureStorage.Remove(PinHashKey);
        await Task.CompletedTask;
#endif
    }

    private static async Task<string?> GetPinHashAsync()
    {
#if WINDOWS
        return await Task.FromResult(GetFromEncryptedFile(PinHashKey));
#else
        return await SecureStorage.GetAsync(PinHashKey);
#endif
    }

    private static string ComputeHash(string pin, string userId)
    {
        var data = Encoding.UTF8.GetBytes($"{userId}:{pin}");
        return Convert.ToBase64String(SHA256.HashData(data));
    }

#if WINDOWS
    private static string GetFilePath(string key) =>
        Path.Combine(FileSystem.AppDataDirectory, $"{key}.dat");

    private static void SaveToEncryptedFile(string key, string value)
    {
        var plainBytes = Encoding.UTF8.GetBytes(value);
        var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(
            plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        File.WriteAllBytes(GetFilePath(key), encryptedBytes);
    }

    private static string? GetFromEncryptedFile(string key)
    {
        try
        {
            var path = GetFilePath(key);
            if (!File.Exists(path)) return null;
            var encryptedBytes = File.ReadAllBytes(path);
            var plainBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                encryptedBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch { return null; }
    }

    private static void ClearEncryptedFile(string key)
    {
        try
        {
            var path = GetFilePath(key);
            if (File.Exists(path)) File.Delete(path);
        }
        catch { }
    }
#endif
}
