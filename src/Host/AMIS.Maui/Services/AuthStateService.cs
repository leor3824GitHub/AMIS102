using System.Text.Json;

namespace AMIS.Maui.Services;

public sealed record EmployeeInfo(
    Guid EmployeeId,
    string FullName,
    string? Department,
    string? Position);

public sealed record UserProfile(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? ImageUrl);

public sealed class AuthStateService
{
    // Identity is stable and non-secret, so it's cached (maui.md: "Employee profile — cached — needed
    // to filter ICS/PAR; stable data"). Persisting it lets a token-resume restore the signed-in user
    // instantly and offline, instead of depending on a network call succeeding during app startup —
    // the most fragile moment (cold HTTP stack, possibly-expired token mid-refresh). Tokens are NOT
    // stored here; they stay in ITokenStorageService (encrypted).
    private const string EmployeeKey = "amis_identity_employee";
    private const string ProfileKey = "amis_identity_profile";

    public EmployeeInfo? Employee { get; private set; }
    public UserProfile? UserProfile { get; private set; }

    public bool IsAuthenticated => UserProfile is not null;

    public void SetEmployee(EmployeeInfo employee)
    {
        Employee = employee;
        Preferences.Default.Set(EmployeeKey, JsonSerializer.Serialize(employee));
    }

    public void SetUserProfile(UserProfile profile)
    {
        UserProfile = profile;
        Preferences.Default.Set(ProfileKey, JsonSerializer.Serialize(profile));
    }

    /// <summary>
    /// Loads identity persisted by a previous session into memory (no network). Only fills fields
    /// that aren't already set, so it's safe to call alongside a live login. Returns true when an
    /// employee identity is available afterwards.
    /// </summary>
    public bool Restore()
    {
        Employee ??= Deserialize<EmployeeInfo>(Preferences.Default.Get(EmployeeKey, ""));
        UserProfile ??= Deserialize<UserProfile>(Preferences.Default.Get(ProfileKey, ""));
        return Employee is not null;
    }

    public void Clear()
    {
        Employee = null;
        UserProfile = null;
        Preferences.Default.Remove(EmployeeKey);
        Preferences.Default.Remove(ProfileKey);
    }

    private static T? Deserialize<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return null;   // corrupted/old-shape cache — treat as absent.
        }
    }
}
