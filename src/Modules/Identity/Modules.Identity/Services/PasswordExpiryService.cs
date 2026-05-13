using AMIS.Modules.Identity.Contracts.DTOs;
using AMIS.Modules.Identity.Contracts.Services;
using AMIS.Modules.Identity.Data;
using AMIS.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.Identity.Services;

internal sealed class PasswordExpiryService : IPasswordExpiryService
{
    private readonly UserManager<AmisUser> _userManager;
    private readonly PasswordPolicyOptions _passwordPolicyOptions;

    public PasswordExpiryService(
        UserManager<AmisUser> userManager,
        IOptions<PasswordPolicyOptions> passwordPolicyOptions)
    {
        _userManager = userManager;
        _passwordPolicyOptions = passwordPolicyOptions.Value;
    }

    public async Task<bool> IsPasswordExpiredAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        return IsPasswordExpired(user);
    }

    public async Task<int> GetDaysUntilExpiryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return int.MaxValue;
        }

        return GetDaysUntilExpiry(user);
    }

    public async Task<bool> IsPasswordExpiringWithinWarningPeriodAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        return IsPasswordExpiringWithinWarningPeriod(user);
    }

    public async Task<PasswordExpiryStatusDto> GetPasswordExpiryStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new PasswordExpiryStatusDto
            {
                IsExpired = false,
                IsExpiringWithinWarningPeriod = false,
                DaysUntilExpiry = int.MaxValue,
                ExpiryDate = null
            };
        }

        return GetPasswordExpiryStatus(user);
    }

    public async Task UpdateLastPasswordChangeDateAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            user.LastPasswordChangeDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
    }

    // Internal helpers that work with AmisUser directly
    private bool IsPasswordExpired(AmisUser user)
    {
        if (!_passwordPolicyOptions.EnforcePasswordExpiry)
        {
            return false;
        }

        var expiryDate = user.LastPasswordChangeDate.AddDays(_passwordPolicyOptions.PasswordExpiryDays);
        return DateTime.UtcNow > expiryDate;
    }

    private int GetDaysUntilExpiry(AmisUser user)
    {
        if (!_passwordPolicyOptions.EnforcePasswordExpiry)
        {
            return int.MaxValue;
        }

        var expiryDate = user.LastPasswordChangeDate.AddDays(_passwordPolicyOptions.PasswordExpiryDays);
        var daysUntilExpiry = (int)(expiryDate - DateTime.UtcNow).TotalDays;
        return daysUntilExpiry;
    }

    private bool IsPasswordExpiringWithinWarningPeriod(AmisUser user)
    {
        if (!_passwordPolicyOptions.EnforcePasswordExpiry)
        {
            return false;
        }

        var daysUntilExpiry = GetDaysUntilExpiry(user);
        return daysUntilExpiry >= 0 && daysUntilExpiry <= _passwordPolicyOptions.PasswordExpiryWarningDays;
    }

    private PasswordExpiryStatusDto GetPasswordExpiryStatus(AmisUser user)
    {
        var expiryDate = user.LastPasswordChangeDate.AddDays(_passwordPolicyOptions.PasswordExpiryDays);
        var daysUntilExpiry = GetDaysUntilExpiry(user);
        var isExpired = IsPasswordExpired(user);
        var isExpiringWithinWarningPeriod = IsPasswordExpiringWithinWarningPeriod(user);

        return new PasswordExpiryStatusDto
        {
            IsExpired = isExpired,
            IsExpiringWithinWarningPeriod = isExpiringWithinWarningPeriod,
            DaysUntilExpiry = daysUntilExpiry,
            ExpiryDate = _passwordPolicyOptions.EnforcePasswordExpiry ? expiryDate : null
        };
    }
}


