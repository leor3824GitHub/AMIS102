using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ThresholdPolicies.GetActiveThresholdPolicy;

public sealed class GetActiveThresholdPolicyQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetActiveThresholdPolicyQuery, ThresholdPolicyDto>
{
    public async ValueTask<ThresholdPolicyDto> Handle(GetActiveThresholdPolicyQuery query, CancellationToken cancellationToken)
    {
        var policy = await dbContext.CapitalizationThresholdPolicies
            .FirstOrDefaultAsync(x => x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (policy is null)
        {
            throw new NotFoundException("No active capitalization threshold policy found. Use the Set Threshold Policy endpoint to configure one.");
        }

        return new ThresholdPolicyDto(
            policy.Id,
            policy.LowValueThreshold,
            policy.CapitalizationThreshold,
            policy.EffectiveDate,
            policy.IsActive,
            policy.Reason,
            policy.CreatedOnUtc,
            policy.CreatedBy);
    }
}
