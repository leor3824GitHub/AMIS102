using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Validators;

public sealed class CreatePurchaseRequestCommandValidatorTests
{
    private readonly CreatePurchaseRequestCommandValidator _sut = new();

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
            PrType = PrType.Unplanned,
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
            PrType = PrType.Unplanned,
            Justification = "Emergency replacement needed"
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
            LineItems = [new CreatePurchaseRequestLineItemRequest(0, "piece", "Bond Paper", 100m)]
        };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_LineItemWithZeroUnitCost_Fails()
    {
        var command = ValidCommand() with
        {
            LineItems = [new CreatePurchaseRequestLineItemRequest(1, "piece", "Bond Paper", 0m)]
        };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    private static CreatePurchaseRequestCommand ValidCommand() =>
        new(
            DepartmentId: Guid.NewGuid(),
            Section: null,
            Purpose: "Purchase of office supplies for Q1",
            PrType: PrType.Planned,
            Justification: null,
            RequestedById: Guid.NewGuid(),
            SaiNumber: null,
            SaiDate: null,
            AlobsNumber: null,
            AlobsDate: null,
            LineItems:
            [
                new CreatePurchaseRequestLineItemRequest(10, "ream", "Bond Paper A4", 250m),
                new CreatePurchaseRequestLineItemRequest(5, "box", "Ballpen black", 80m)
            ]);
}

