using AMIS.Modules.Expendable.Contracts.v1.Cart;
using AMIS.Modules.Expendable.Domain.Cart;

namespace AMIS.Modules.Expendable.Features.v1.Cart;

internal static class CartMapper
{
    internal static EmployeeShoppingCartDto ToEmployeeShoppingCartDto(this EmployeeShoppingCart cart) =>
        new(
            cart.Id,
            cart.EmployeeId,
            cart.Status.ToString(),
            cart.GetCartTotal(),
            cart.GetTotalItemCount(),
            cart.Items.Select(x => new CartItemDto(
                x.ProductId,
                x.Quantity,
                x.UnitPrice,
                x.LineTotal)).ToList(),
            cart.CreatedOnUtc);
}


