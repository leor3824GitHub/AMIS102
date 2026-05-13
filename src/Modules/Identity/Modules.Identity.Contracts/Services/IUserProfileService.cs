using AMIS.Framework.Shared.Storage;
using AMIS.Modules.Identity.Contracts.DTOs;

namespace AMIS.Modules.Identity.Contracts.Services;

/// <summary>
/// Service for user profile operations.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<UserDto> GetAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all users.
    /// </summary>
    Task<List<UserDto>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the total user count.
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates a user's profile.
    /// </summary>
    Task UpdateAsync(string userId, string firstName, string lastName, string phoneNumber, FileUploadRequest? image, bool deleteCurrentImage);

    /// <summary>
    /// Checks if a user exists with the given email.
    /// </summary>
    Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null);

    /// <summary>
    /// Checks if a user exists with the given username.
    /// </summary>
    Task<bool> ExistsWithNameAsync(string name);

    /// <summary>
    /// Checks if a user exists with the given phone number.
    /// </summary>
    Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null);
}

