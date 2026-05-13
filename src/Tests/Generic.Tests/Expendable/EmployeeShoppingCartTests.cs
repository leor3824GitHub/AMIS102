using AMIS.Modules.Expendable.Domain.Cart;
using AMIS.Modules.Expendable.Features.v1.Cart;

namespace Generic.Tests.Expendable;

public sealed class EmployeeShoppingCartTests
{
    [Fact]
    public void UpdateItemQuantity_WhenSetToZero_RemovesItem()
    {
        // Arrange
        var cart = EmployeeShoppingCart.Create("tenant-1", "employee-1");
        var productId = Guid.NewGuid();
        cart.AddItem(productId, 3, 10m);

        // Act
        cart.UpdateItemQuantity(productId, 0);

        // Assert
        cart.Items.Count.ShouldBe(0);
    }

    [Fact]
    public void ConvertToRequest_WhenCartIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var cart = EmployeeShoppingCart.Create("tenant-1", "employee-1");

        // Act
        var action = () => cart.ConvertToRequest(Guid.NewGuid());

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void UpdateCartItemQuantityValidator_WhenQuantityIsZero_IsValid()
    {
        // Arrange
        var validator = new UpdateCartItemQuantityCommandValidator();
        var command = new AMIS.Modules.Expendable.Contracts.v1.Cart.UpdateCartItemQuantityCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}

