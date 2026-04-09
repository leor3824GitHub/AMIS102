using FSH.Modules.MasterData.Domain;
using FSH.Modules.MasterData.Features.v1.Suppliers.CreateSupplier;
using FSH.Modules.MasterData.Features.v1.Suppliers.UpdateSupplier;

namespace Generic.Tests.MasterData;

public sealed class SupplierDomainAndValidatorTests
{
    [Theory]
    [InlineData("vat", "VAT")]
    [InlineData("VAT", "VAT")]
    [InlineData("non-vat", "NON-VAT")]
    [InlineData("NON-VAT", "NON-VAT")]
    [InlineData("invalid-value", "NON-VAT")]
    public void Create_WithBusinessTaxType_NormalizesToCanonicalValues(string input, string expected)
    {
        // Arrange + Act
        var supplier = Supplier.Create("SUP-001", "Acme", null, input, null, null, null, null, null);

        // Assert
        supplier.BusinessTaxType.ShouldBe(expected);
    }

    [Fact]
    public void Update_WithLegacyOverload_DefaultsBusinessTaxTypeToNonVat()
    {
        // Arrange
        var supplier = Supplier.Create("SUP-001", "Acme", "123", "VAT", null, null, null, null, null);

        // Act
        supplier.Update("SUP-001", "Acme Updated", null, null, null, null, null, true);

        // Assert
        supplier.BusinessTaxType.ShouldBe("NON-VAT");
        supplier.TinNo.ShouldBeNull();
    }

    [Fact]
    public void CreateValidator_WithInvalidBusinessTaxType_FailsValidation()
    {
        // Arrange
        var validator = new CreateSupplierCommandValidator();
        var command = new CreateSupplierCommand("SUP-001", "Acme", "123", "WRONG");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Any(e => e.PropertyName == nameof(CreateSupplierCommand.BusinessTaxType)).ShouldBeTrue();
    }

    [Fact]
    public void UpdateValidator_WithValidLowerCaseBusinessTaxType_PassesValidation()
    {
        // Arrange
        var validator = new UpdateSupplierCommandValidator();
        var command = new UpdateSupplierCommand(Guid.NewGuid(), "SUP-001", "Acme", "123", "vat");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}
