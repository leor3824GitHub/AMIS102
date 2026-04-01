using FSH.Framework.Core.Context;
using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.Expendable.Data;
using FSH.Modules.Expendable.Domain.Requests;
using Mediator;

namespace FSH.Modules.Expendable.Features.v1.Requests.CreateSupplyRequest;

public sealed class CreateSupplyRequestCommandHandler : ICommandHandler<CreateSupplyRequestCommand, SupplyRequestDto>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateSupplyRequestCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<SupplyRequestDto> Handle(CreateSupplyRequestCommand command, CancellationToken cancellationToken)
    {
        var neededByUtc = command.NeededByDate?.ToUniversalTime();

        // Generate request number (simplified - use your own logic)
        var requestNumber = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}";

        var request = SupplyRequest.Create(
            _currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required"),
            requestNumber,
            _currentUser.GetUserId().ToString(),
            command.DepartmentId,
            command.BusinessJustification,
            neededByUtc);

        request.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.SupplyRequests.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return request.ToSupplyRequestDto();
    }
}
