using AMIS.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.CreateRRSP;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Validators;

public sealed class CreateRRSPCommandValidatorTests
{
    private readonly CreateRRSPCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyRRSPNo_Fails()
    {
        var command = ValidCommand() with { RRSPNo = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.RRSPNo));
    }

    [Fact]
    public void Validate_RRSPNoExceeds32Characters_Fails()
    {
        var command = ValidCommand() with { RRSPNo = "RRP-2024-01-" + new string('0', 21) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.RRSPNo));
    }

    [Theory]
    [InlineData("RRP-2024-01-0001")]
    [InlineData("RRP-2025-12-9999")]
    [InlineData("RRP-2000-06-0100")]
    public void Validate_ValidRRSPNoFormat_Passes(string rrspNo)
    {
        var command = ValidCommand() with { RRSPNo = rrspNo };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("RRSP-2024-01-0001")]
    [InlineData("RRP-24-01-0001")]
    [InlineData("RRP-2024-1-0001")]
    [InlineData("RRP-2024-01-001")]
    [InlineData("rrp-2024-01-0001")]
    [InlineData("RRP/2024/01/0001")]
    public void Validate_InvalidRRSPNoFormat_Fails(string rrspNo)
    {
        var command = ValidCommand() with { RRSPNo = rrspNo };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.RRSPNo));
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
    public void Validate_EmptyICSId_Fails()
    {
        var command = ValidCommand() with { ICSId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ICSId));
    }

    [Fact]
    public void Validate_FundClusterExceeds50Characters_Fails()
    {
        var command = ValidCommand() with { FundCluster = new string('F', 51) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.FundCluster));
    }

    [Fact]
    public void Validate_NullFundCluster_Passes()
    {
        var command = ValidCommand() with { FundCluster = null };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyReturnedByEmployeeId_Fails()
    {
        var command = ValidCommand() with { ReturnedByEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReturnedByEmployeeId));
    }

    [Fact]
    public void Validate_RemarksExceeds500Characters_Fails()
    {
        var command = ValidCommand() with { Remarks = new string('R', 501) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Remarks));
    }

    [Fact]
    public void Validate_NullRemarks_Passes()
    {
        var command = ValidCommand() with { Remarks = null };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    private static CreateRRSPCommand ValidCommand() =>
        new(
            RRSPNo: "RRP-2025-01-0001",
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            ICSId: Guid.NewGuid(),
            FundCluster: "101",
            ReceivedByEmployeeId: Guid.NewGuid(),
            ReturnedByEmployeeId: Guid.NewGuid(),
            Remarks: null);
}

