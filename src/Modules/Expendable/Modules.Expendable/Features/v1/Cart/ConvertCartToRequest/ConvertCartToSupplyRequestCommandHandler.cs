using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Cart;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Cart;
using AMIS.Modules.Expendable.Domain.Requests;
using AMIS.Modules.Expendable.Features.v1.Requests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Cart.ConvertCartToRequest;

public sealed class ConvertCartToSupplyRequestCommandHandler : ICommandHandler<ConvertCartToSupplyRequestCommand, SupplyRequestDto>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ConvertCartToSupplyRequestCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<SupplyRequestDto> Handle(ConvertCartToSupplyRequestCommand command, CancellationToken cancellationToken)
    {
        var neededByUtc = command.NeededByDate?.ToUniversalTime();

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var cart = await _dbContext.ShoppingCarts
            .FirstOrDefaultAsync(c => c.Id == command.CartId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Cart {command.CartId} not found.");

        // Create supply request
        var requestNumber = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}";
        var request = SupplyRequest.Create(
            _currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required"),
            requestNumber,
            _currentUser.GetUserId().ToString(),
            command.DepartmentId,
            command.BusinessJustification,
            neededByUtc);

        request.CreatedBy = _currentUser.GetUserId().ToString();

        // Add items from cart
        foreach (var item in cart.Items)
        {
            request.AddItem(item.ProductId, item.Quantity);
        }

        // Cart checkout should create a request that is immediately visible for approval.
        request.Submit();
        request.LastModifiedBy = _currentUser.GetUserId().ToString();

        _dbContext.SupplyRequests.Add(request);
        cart.ConvertToRequest(request.Id);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return request.ToSupplyRequestDto();
    }
}

