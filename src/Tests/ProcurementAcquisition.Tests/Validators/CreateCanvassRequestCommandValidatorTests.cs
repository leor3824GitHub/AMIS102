using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.CreateCanvassRequest;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Validators;

public sealed class CreateCanvassRequestCommandValidatorTests
{
    private readonly CreateCanvassRequestCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyPurchaseRequestId_Fails()
    {
        var command = ValidCommand() with { PurchaseRequestId = Guid.Empty };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PurchaseRequestId));
    }

    [Fact]
    public void Validate_PastReturnDeadline_Fails()
    {
        var command = ValidCommand() with { ReturnDeadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReturnDeadline));
    }

    [Fact]
    public void Validate_NoSelectedLines_Fails()
    {
        var command = ValidCommand() with { PrItemNos = [] };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PrItemNos));
    }

    private static CreateCanvassRequestCommand ValidCommand() =>
        new(
            PurchaseRequestId: Guid.NewGuid(),
            ReturnDeadline: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            PrItemNos: [1, 2]);
}
