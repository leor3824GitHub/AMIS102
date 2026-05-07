namespace Playground.Maui.Services;

public interface IPinStorageService
{
    Task<bool> IsPinSetAsync();
    Task SavePinAsync(string pin, string userId);
    Task<bool> VerifyPinAsync(string pin, string userId);
    Task ClearAsync();
}
