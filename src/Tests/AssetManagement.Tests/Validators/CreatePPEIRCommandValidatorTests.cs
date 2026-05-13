using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.AssetManagement.Features.v1.PPEIssuanceReports.CreatePPEIR;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Validators;

public sealed class CreatePPEIRCommandValidatorTests
{
    private readonly CreatePPEIRCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyPPEIRNo_Fails()
    {
        var command = ValidCommand() with { PPEIRNo = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PPEIRNo));
    }

    [Fact]
    public void Validate_PPEIRNoExceeds32Characters_Fails()
    {
        var command = ValidCommand() with { PPEIRNo = new string('P', 33) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PPEIRNo));
    }

    [Fact]
    public void Validate_DefaultDate_Fails()
    {
        var command = ValidCommand() with { Date = default };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Date));
    }

    [Fact]
    public void Validate_EmptyIssuedToEmployeeId_Fails()
    {
        var command = ValidCommand() with { IssuedToEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.IssuedToEmployeeId));
    }

    [Fact]
    public void Validate_EmptyIssuedToOfficeAddress_Fails()
    {
        var command = ValidCommand() with { IssuedToOfficeAddress = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.IssuedToOfficeAddress));
    }

    [Fact]
    public void Validate_IssuedToOfficeAddressExceeds500Characters_Fails()
    {
        var command = ValidCommand() with { IssuedToOfficeAddress = new string('A', 501) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.IssuedToOfficeAddress));
    }

    [Theory]
    [InlineData(PPEIssuanceType.TransferCO)]
    [InlineData(PPEIssuanceType.TransferRO)]
    [InlineData(PPEIssuanceType.TransferPO)]
    [InlineData(PPEIssuanceType.Donation)]
    [InlineData(PPEIssuanceType.Sale)]
    public void Validate_ValidIssuanceType_Passes(PPEIssuanceType issuanceType)
    {
        var command = ValidCommand() with { IssuanceType = issuanceType };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_InvalidIssuanceType_Fails()
    {
        var command = ValidCommand() with { IssuanceType = (PPEIssuanceType)99 };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.IssuanceType));
    }

    [Fact]
    public void Validate_EmptyIssuedByEmployeeId_Fails()
    {
        var command = ValidCommand() with { IssuedByEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.IssuedByEmployeeId));
    }

    [Fact]
    public void Validate_EmptyReceivedByEmployeeId_Fails()
    {
        var command = ValidCommand() with { ReceivedByEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReceivedByEmployeeId));
    }

    [Fact]
    public void Validate_EmptyApprovedByEmployeeId_Fails()
    {
        var command = ValidCommand() with { ApprovedByEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ApprovedByEmployeeId));
    }

    [Fact]
    public void Validate_DriverNameExceeds200Characters_Fails()
    {
        var command = ValidCommand() with { DriverName = new string('D', 201) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.DriverName));
    }

    [Fact]
    public void Validate_NullDriverName_Passes()
    {
        var command = ValidCommand() with { DriverName = null };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_BillOfLadingNoExceeds100Characters_Fails()
    {
        var command = ValidCommand() with { BillOfLadingNo = new string('B', 101) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.BillOfLadingNo));
    }

    [Fact]
    public void Validate_NullBillOfLadingNo_Passes()
    {
        var command = ValidCommand() with { BillOfLadingNo = null };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyItems_Fails()
    {
        var command = ValidCommand() with { Items = [] };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Items));
    }

    [Fact]
    public void Validate_ItemWithEmptyTangibleInventoryItemId_Fails()
    {
        var command = ValidCommand() with
        {
            Items = [new CreatePPEIRItemRequest(Guid.Empty)]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("TangibleInventoryItemId"));
    }

    [Fact]
    public void Validate_MultipleValidItems_Passes()
    {
        var command = ValidCommand() with
        {
            Items =
            [
                new CreatePPEIRItemRequest(Guid.NewGuid()),
                new CreatePPEIRItemRequest(Guid.NewGuid()),
            ]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    private static CreatePPEIRCommand ValidCommand() =>
        new(
            PPEIRNo: "PPEIR-2025-01-001",
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            IssuedToEmployeeId: Guid.NewGuid(),
            IssuedToOfficeAddress: "123 Main Street, City",
            IssuanceType: PPEIssuanceType.TransferCO,
            IssuedByEmployeeId: Guid.NewGuid(),
            ReceivedByEmployeeId: Guid.NewGuid(),
            ApprovedByEmployeeId: Guid.NewGuid(),
            DateReceived: null,
            DriverName: null,
            BillOfLadingNo: null,
            Items: [new CreatePPEIRItemRequest(Guid.NewGuid())]);
}

