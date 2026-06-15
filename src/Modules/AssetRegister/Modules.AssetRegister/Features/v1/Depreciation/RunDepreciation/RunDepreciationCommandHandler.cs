using AMIS.Modules.AssetRegister.Contracts.v1.Depreciation;
using AMIS.Modules.AssetRegister.Data.Services;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Depreciation.RunDepreciation;

public sealed class RunDepreciationCommandHandler(DepreciationPostingService service)
    : ICommandHandler<RunDepreciationCommand, RunDepreciationResultDto>
{
    public async ValueTask<RunDepreciationResultDto> Handle(RunDepreciationCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var period = cmd.AsOfPeriod ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return await service.PostThroughAsync(period, cancellationToken).ConfigureAwait(false);
    }
}
