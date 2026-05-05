namespace Playground.Maui.Services;

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
    public EmployeeInfo? Employee { get; private set; }
    public UserProfile? UserProfile { get; private set; }

    public bool IsAuthenticated => UserProfile is not null;

    public void SetEmployee(EmployeeInfo employee) => Employee = employee;
    public void SetUserProfile(UserProfile profile) => UserProfile = profile;

    public void Clear()
    {
        Employee = null;
        UserProfile = null;
    }
}
