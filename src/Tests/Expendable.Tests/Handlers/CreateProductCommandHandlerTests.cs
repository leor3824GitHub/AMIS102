using AutoFixture;
using Shouldly;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Features.v1.Products.CreateProduct;
using NSubstitute;
using Xunit;

namespace Expendable.Tests.Handlers;

/// <summary>
/// Tests for CreateProductCommandHandler - validates product creation via API.
/// Tests focus on validator business rules and command validation.
/// </summary>
public sealed class CreateProductCommandHandlerTests
{
    private readonly ICurrentUser _currentUserMock;
    private readonly Fixture _fixture;

    public CreateProductCommandHandlerTests()
    {
        _currentUserMock = Substitute.For<ICurrentUser>();
        _fixture = new Fixture();

        // Setup default current user
        _currentUserMock.GetTenant().Returns("test-tenant-id");
        _currentUserMock.GetUserId().Returns(Guid.NewGuid());
    }

    /// <summary>
    /// Test: CreateProductCommand with valid data should pass validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_ValidCommand_ShouldPass()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            SKU: "TEST-SKU-001",
            Name: "Test Product",
            Description: "Test Description",
            UnitPrice: 99.99m,
            UnitOfMeasure: "Each",
            MinimumStockLevel: 10,
            ReorderQuantity: 50);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// Test: CreateProduct with missing SKU should fail validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_EmptySKU_ShouldFail()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = _fixture.Build<CreateProductCommand>()
            .With(c => c.SKU, string.Empty)
            .With(c => c.Name, "Test Product")
            .With(c => c.Description, "Test Description")
            .With(c => c.UnitPrice, 99.99m)
            .With(c => c.UnitOfMeasure, "Each")
            .With(c => c.MinimumStockLevel, 10)
            .With(c => c.ReorderQuantity, 50)
            .Create();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.SKU));
    }

    /// <summary>
    /// Test: CreateProduct with SKU exceeding max length should fail validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_SKUExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = _fixture.Build<CreateProductCommand>()
            .With(c => c.SKU, new string('A', 51)) // Max length is 50
            .With(c => c.Name, "Test Product")
            .With(c => c.Description, "Test Description")
            .With(c => c.UnitPrice, 99.99m)
            .With(c => c.UnitOfMeasure, "Each")
            .With(c => c.MinimumStockLevel, 10)
            .With(c => c.ReorderQuantity, 50)
            .Create();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.SKU));
    }

    /// <summary>
    /// Test: CreateProduct with missing product name should fail validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_EmptyName_ShouldFail()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = _fixture.Build<CreateProductCommand>()
            .With(c => c.SKU, "TEST-SKU")
            .With(c => c.Name, string.Empty)
            .With(c => c.Description, "Test Description")
            .With(c => c.UnitPrice, 99.99m)
            .With(c => c.UnitOfMeasure, "Each")
            .With(c => c.MinimumStockLevel, 10)
            .With(c => c.ReorderQuantity, 50)
            .Create();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Name));
    }

    /// <summary>
    /// Test: CreateProduct with negative unit price should fail validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_NegativeUnitPrice_ShouldFail()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = _fixture.Build<CreateProductCommand>()
            .With(c => c.SKU, "TEST-SKU")
            .With(c => c.Name, "Test Product")
            .With(c => c.Description, "Test Description")
            .With(c => c.UnitPrice, -10m) // Negative price
            .With(c => c.UnitOfMeasure, "Each")
            .With(c => c.MinimumStockLevel, 10)
            .With(c => c.ReorderQuantity, 50)
            .Create();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.UnitPrice));
    }

    /// <summary>
    /// Test: CreateProduct with zero unit price should fail validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_ZeroUnitPrice_ShouldFail()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = _fixture.Build<CreateProductCommand>()
            .With(c => c.SKU, "TEST-SKU")
            .With(c => c.Name, "Test Product")
            .With(c => c.Description, "Test Description")
            .With(c => c.UnitPrice, 0m) // Zero price
            .With(c => c.UnitOfMeasure, "Each")
            .With(c => c.MinimumStockLevel, 10)
            .With(c => c.ReorderQuantity, 50)
            .Create();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.UnitPrice));
    }

    /// <summary>
    /// Test: CreateProduct with missing unit of measure should fail validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_EmptyUnitOfMeasure_ShouldFail()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = _fixture.Build<CreateProductCommand>()
            .With(c => c.SKU, "TEST-SKU")
            .With(c => c.Name, "Test Product")
            .With(c => c.Description, "Test Description")
            .With(c => c.UnitPrice, 99.99m)
            .With(c => c.UnitOfMeasure, string.Empty) // Empty unit of measure
            .With(c => c.MinimumStockLevel, 10)
            .With(c => c.ReorderQuantity, 50)
            .Create();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.UnitOfMeasure));
    }

    /// <summary>
    /// Test: CreateProduct with negative minimum stock level should fail validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_NegativeMinimumStockLevel_ShouldFail()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = _fixture.Build<CreateProductCommand>()
            .With(c => c.SKU, "TEST-SKU")
            .With(c => c.Name, "Test Product")
            .With(c => c.Description, "Test Description")
            .With(c => c.UnitPrice, 99.99m)
            .With(c => c.UnitOfMeasure, "Each")
            .With(c => c.MinimumStockLevel, -5) // Negative stock level
            .With(c => c.ReorderQuantity, 50)
            .Create();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.MinimumStockLevel));
    }

    /// <summary>
    /// Test: CreateProduct with zero or negative reorder quantity should fail validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_ZeroReorderQuantity_ShouldFail()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = _fixture.Build<CreateProductCommand>()
            .With(c => c.SKU, "TEST-SKU")
            .With(c => c.Name, "Test Product")
            .With(c => c.Description, "Test Description")
            .With(c => c.UnitPrice, 99.99m)
            .With(c => c.UnitOfMeasure, "Each")
            .With(c => c.MinimumStockLevel, 10)
            .With(c => c.ReorderQuantity, 0) // Zero reorder quantity
            .Create();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReorderQuantity));
    }

    /// <summary>
    /// Test: CreateProduct with ParentProductId set but no VariantName should fail validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_WithParentProductId_AndEmptyVariantName_ShouldFail()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = _fixture.Build<CreateProductCommand>()
            .With(c => c.SKU, "VARIANT-SKU-001")
            .With(c => c.Name, "Test Variant Product")
            .With(c => c.Description, "Test Description")
            .With(c => c.UnitPrice, 99.99m)
            .With(c => c.UnitOfMeasure, "Each")
            .With(c => c.MinimumStockLevel, 10)
            .With(c => c.ReorderQuantity, 50)
            .With(c => c.ParentProductId, Guid.NewGuid())
            .With(c => c.VariantName, string.Empty) // Empty variant name with parent set
            .Create();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.VariantName));
    }

    /// <summary>
    /// Test: CreateProduct with ParentProductId set and valid VariantName should pass validation
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_WithParentProductId_AndValidVariantName_ShouldPass()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            SKU: "VARIANT-SKU-001",
            Name: "Test Variant Product",
            Description: "Test Description",
            UnitPrice: 99.99m,
            UnitOfMeasure: "Each",
            MinimumStockLevel: 10,
            ReorderQuantity: 50,
            ParentProductId: Guid.NewGuid(),
            VariantName: "Red / Large");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// Test: CreateProduct without ParentProductId should not require VariantName
    /// </summary>
    [Fact]
    public void CreateProductCommandValidator_WithoutParentProductId_AndNoVariantName_ShouldPass()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            SKU: "BASE-SKU-001",
            Name: "Base Product",
            Description: "Test Description",
            UnitPrice: 99.99m,
            UnitOfMeasure: "Each",
            MinimumStockLevel: 10,
            ReorderQuantity: 50); // ParentProductId defaults to null — VariantName rule does not apply

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}


