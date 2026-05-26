namespace AMIS.Playground.Blazor.Services;

/// <summary>
/// Service for managing and sharing user profile state across components.
/// When the profile is updated (e.g., in ProfileSettings), other components
/// like the layout header can subscribe to be notified of changes.
/// </summary>
#pragma warning disable CA1056, CA1054 // Avatar URLs are passed as strings from APIs
#pragma warning disable CA1003 // Action is the idiomatic pattern for Blazor state change events
internal interface IUserProfileState
{
    string UserName { get; }
    string? UserEmail { get; }
    string? UserRole { get; }
    string? AvatarUrl { get; }

    /// <summary>Employee record ID for the current identity user. Guid.Empty when not resolved.</summary>
    Guid EmployeeId { get; }

    /// <summary>Full name (FirstName + LastName) of the linked employee. Empty when not resolved.</summary>
    string EmployeeFullName { get; }

    /// <summary>Position/designation of the linked employee. Empty when not resolved.</summary>
    string EmployeePositionName { get; }

    /// <summary>Permission strings granted to the current user. Empty set until loaded.</summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>
    /// Event raised when profile data changes.
    /// </summary>
    event Action? OnProfileChanged;

    /// <summary>
    /// Updates the profile state and notifies subscribers.
    /// </summary>
    void UpdateProfile(string userName, string? userEmail, string? userRole, string? avatarUrl);

    void SetEmployeeProfile(Guid employeeId, string fullName, string positionName);
    void SetPermissions(IReadOnlySet<string> permissions);

    /// <summary>
    /// Clears the profile state (e.g., on logout).
    /// </summary>
    void Clear();
}
#pragma warning restore CA1003
#pragma warning restore CA1056, CA1054

internal sealed class UserProfileState : IUserProfileState
{
    public string UserName { get; private set; } = "User";
    public string? UserEmail { get; private set; }
    public string? UserRole { get; private set; }
    public string? AvatarUrl { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeFullName { get; private set; } = string.Empty;
    public string EmployeePositionName { get; private set; } = string.Empty;
    public IReadOnlySet<string> Permissions { get; private set; } = new HashSet<string>();

    public event Action? OnProfileChanged;

    public void UpdateProfile(string userName, string? userEmail, string? userRole, string? avatarUrl)
    {
        UserName = userName;
        UserEmail = userEmail;
        UserRole = userRole;
        AvatarUrl = avatarUrl;
        OnProfileChanged?.Invoke();
    }

    public void SetEmployeeProfile(Guid employeeId, string fullName, string positionName)
    {
        EmployeeId = employeeId;
        EmployeeFullName = fullName;
        EmployeePositionName = positionName;
    }

    public void SetPermissions(IReadOnlySet<string> permissions) => Permissions = permissions;

    public void Clear()
    {
        UserName = "User";
        UserEmail = null;
        UserRole = null;
        AvatarUrl = null;
        EmployeeId = Guid.Empty;
        EmployeeFullName = string.Empty;
        EmployeePositionName = string.Empty;
        Permissions = new HashSet<string>();
        OnProfileChanged?.Invoke();
    }
}
