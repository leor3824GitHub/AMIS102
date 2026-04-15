using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ThresholdPolicies.SetThresholdPolicy;

public sealed class SetThresholdPolicyCommandHandler : ICommandHandler<SetThresholdPolicyCommand, SetThresholdPolicyResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public SetThresholdPolicyCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<SetThresholdPolicyResult> Handle(SetThresholdPolicyCommand command, CancellationToken cancellationToken)
    {
        // Deactivate any currently active policy
        var current = await _dbContext.CapitalizationThresholdPolicies
            .FirstOrDefaultAsync(x => x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (current is not null)
        {
            current.Deactivate();
        }

        var policy = CapitalizationThresholdPolicy.Create(
            command.LowValueThreshold,
            command.CapitalizationThreshold,
            command.EffectiveDate,
            command.Reason);

        policy.CreatedBy = _currentUser.GetUserId().ToString();
        _dbContext.CapitalizationThresholdPolicies.Add(policy);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SetThresholdPolicyResult(
            policy.Id,
            policy.LowValueThreshold,
            policy.CapitalizationThreshold,
            policy.EffectiveDate);
    }
}
