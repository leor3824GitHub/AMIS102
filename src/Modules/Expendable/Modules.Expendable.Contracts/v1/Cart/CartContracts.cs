using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using Mediator;

namespace AMIS.Modules.Expendable.Contracts.v1.Cart;

public record CartItemDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record EmployeeShoppingCartDto(
    Guid Id,
    string EmployeeId,
    string Status,
    decimal CartTotal,
    int ItemCount,
    List<CartItemDto> Items,
    DateTimeOffset CreatedOnUtc);

public record GetOrCreateCartCommand(string EmployeeId) : ICommand<EmployeeShoppingCartDto>;

public record AddToCartCommand(
    Guid CartId,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice) : ICommand<Unit>;

public record UpdateCartItemQuantityCommand(
    Guid CartId,
    Guid ProductId,
    int NewQuantity) : ICommand<Unit>;

public record RemoveFromCartCommand(
    Guid CartId,
    Guid ProductId) : ICommand<Unit>;

public record ConvertCartToSupplyRequestCommand(
    Guid CartId,
    string DepartmentId,
    string? BusinessJustification = null,
    DateTimeOffset? NeededByDate = null) : ICommand<SupplyRequestDto>;

public record ClearCartCommand(Guid CartId) : ICommand<Unit>;

public record GetEmployeeCartQuery(string EmployeeId) : IQuery<EmployeeShoppingCartDto?>;

public record GetCartQuery(Guid CartId) : IQuery<EmployeeShoppingCartDto?>;


