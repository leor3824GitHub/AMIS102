using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.CreateAssetPurchaseRequest;
using Shouldly;
using Xunit;

namespace AssetProcurement.Tests.Validators;

public sealed class CreateAssetPurchaseRequestCommandValidatorTests
{
    private readonly CreateAssetPurchaseRequestCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = ValidCommand();

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyDepartmentId_Fails()
    {
        var command = ValidCommand() with { DepartmentId = Guid.Empty };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.DepartmentId));
    }

    [Fact]
    public void Validate_EmptyPurpose_Fails()
    {
        var command = ValidCommand() with { Purpose = string.Empty };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Purpose));
    }

    [Fact]
    public void Validate_EmptyRequestedById_Fails()
    {
        var command = ValidCommand() with { RequestedById = Guid.Empty };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.RequestedById));
    }

    [Fact]
    public void Validate_UnplannedWithoutJustification_Fails()
    {
        var command = ValidCommand() with
        {
            PrType = AssetPrType.Unplanned,
            Justification = null
        };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Justification));
    }

    [Fact]
    public void Validate_UnplannedWithJustification_Passes()
    {
        var command = ValidCommand() with
        {
            PrType = AssetPrType.Unplanned,
            Justification = "Emergency replacement required"
        };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyLineItems_Fails()
    {
        var command = ValidCommand() with { LineItems = [] };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.LineItems));
    }

    [Fact]
    public void Validate_LineItemWithZeroQuantity_Fails()
    {
        var command = ValidCommand() with
        {
            LineItems = [new AssetPurchaseRequestLineItemRequest(
                "Laptop",
                null, null, null, null,
                "Unit",
                Quantity: 0,
                EstimatedUnitCost: 50000m)]
        };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_LineItemWithZeroCost_Fails()
    {
        var command = ValidCommand() with
        {
            LineItems = [new AssetPurchaseRequestLineItemRequest(
                "Laptop",
                null, null, null, null,
                "Unit",
                Quantity: 1,
                EstimatedUnitCost: 0m)]
        };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    private static CreateAssetPurchaseRequestCommand ValidCommand() =>
        new(
            DepartmentId: Guid.NewGuid(),
            Section: null,
            Purpose: "Replacement of old laptops",
            PrType: AssetPrType.Planned,
            Justification: null,
            RequestedById: Guid.NewGuid(),
            SaiNumber: null,
            SaiDate: null,
            AlobsNumber: null,
            AlobsDate: null,
            LineItems:
            [
                new AssetPurchaseRequestLineItemRequest(
                    "Laptop Dell XPS 15",
                    "For software development",
                    "Dell",
                    "XPS 15",
                    null,
                    "Unit",
                    Quantity: 3,
                    EstimatedUnitCost: 75000m)
            ]);
}

