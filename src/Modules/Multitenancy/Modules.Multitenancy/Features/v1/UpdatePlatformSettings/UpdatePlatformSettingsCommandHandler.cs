using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Multitenancy.Contracts.v1.UpdatePlatformSettings;
using Mediator;

namespace AMIS.Modules.Multitenancy.Features.v1.UpdatePlatformSettings;

public sealed class UpdatePlatformSettingsCommandHandler(IPlatformSettingsService settingsService)
    : ICommandHandler<UpdatePlatformSettingsCommand>
{
    public async ValueTask<Unit> Handle(UpdatePlatformSettingsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await settingsService.UpdateAsync(command.Settings, cancellationToken);

        return Unit.Value;
    }
}
