using AMIS.Modules.Multitenancy.Contracts.Dtos;
using AMIS.Modules.Multitenancy.Contracts.v1.UpdatePlatformSettings;
using FluentValidation;

namespace AMIS.Modules.Multitenancy.Features.v1.UpdatePlatformSettings;

public sealed class UpdatePlatformSettingsCommandValidator : AbstractValidator<UpdatePlatformSettingsCommand>
{
    public UpdatePlatformSettingsCommandValidator()
    {
        RuleFor(x => x.Settings).NotNull();

        RuleFor(x => x.Settings.Session)
            .NotNull()
            .SetValidator(new SessionSettingsValidator());

        RuleFor(x => x.Settings.Quota)
            .NotNull()
            .SetValidator(new QuotaSettingsValidator());
    }

    private sealed class SessionSettingsValidator : AbstractValidator<SessionSettingsDto>
    {
        public SessionSettingsValidator()
        {
            RuleFor(x => x.MaxSessionsPerUser)
                .InclusiveBetween(1, 100)
                .When(x => x.MaxSessionsPerUser.HasValue)
                .WithMessage("Max sessions per user must be between 1 and 100, or empty for unlimited.");

            RuleFor(x => x.IdleTimeoutMinutes)
                .InclusiveBetween(1, 1440)
                .When(x => x.IdleTimeoutMinutes.HasValue)
                .WithMessage("Idle timeout must be between 1 and 1440 minutes, or empty for none.");

            RuleFor(x => x.AbsoluteTimeoutDays)
                .InclusiveBetween(1, 365)
                .WithMessage("Absolute timeout must be between 1 and 365 days.");
        }
    }

    private sealed class QuotaSettingsValidator : AbstractValidator<QuotaSettingsDto>
    {
        public QuotaSettingsValidator()
        {
            RuleFor(x => x.MaxUsersPerTenant)
                .GreaterThan(0)
                .When(x => x.MaxUsersPerTenant.HasValue)
                .WithMessage("Max users per tenant must be greater than 0, or empty for unlimited.");

            RuleFor(x => x.StorageLimitMb)
                .GreaterThan(0)
                .When(x => x.StorageLimitMb.HasValue)
                .WithMessage("Storage limit must be greater than 0 MB, or empty for unlimited.");

            RuleFor(x => x.ApiRateLimitPerMinute)
                .InclusiveBetween(1, 100000)
                .When(x => x.ApiRateLimitPerMinute.HasValue)
                .WithMessage("API rate limit must be between 1 and 100000 requests/min, or empty for none.");
        }
    }
}
