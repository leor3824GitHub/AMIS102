using AMIS.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace AMIS.Modules.Multitenancy.Contracts.v1.UpdatePlatformSettings;

public sealed record UpdatePlatformSettingsCommand(PlatformSettingsDto Settings) : ICommand;
