using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.CreatePhysicalCountSession;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Validators;

public sealed class CreatePhysicalCountSessionCommandValidatorTests
{
    private readonly CreatePhysicalCountSessionCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptySessionNo_Fails()
    {
        var command = ValidCommand() with { SessionNo = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.SessionNo));
    }

    [Fact]
    public void Validate_SessionNoExceeds32Characters_Fails()
    {
        var command = ValidCommand() with { SessionNo = new string('S', 33) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.SessionNo));
    }

    [Fact]
    public void Validate_SessionNoExactly32Characters_Passes()
    {
        var command = ValidCommand() with { SessionNo = new string('S', 32) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DefaultCountDate_Fails()
    {
        var command = ValidCommand() with { CountDate = default };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.CountDate));
    }

    [Fact]
    public void Validate_EmptyStationOffice_Fails()
    {
        var command = ValidCommand() with { StationOffice = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.StationOffice));
    }

    [Fact]
    public void Validate_StationOfficeExceeds200Characters_Fails()
    {
        var command = ValidCommand() with { StationOffice = new string('O', 201) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.StationOffice));
    }

    [Fact]
    public void Validate_StationOfficeExactly200Characters_Passes()
    {
        var command = ValidCommand() with { StationOffice = new string('O', 200) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(PhysicalCountScope.PPEOnly)]
    [InlineData(PhysicalCountScope.SemiExpendableOnly)]
    [InlineData(PhysicalCountScope.Both)]
    public void Validate_ValidScope_Passes(PhysicalCountScope scope)
    {
        var command = ValidCommand() with { Scope = scope };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_InvalidScope_Fails()
    {
        var command = ValidCommand() with { Scope = (PhysicalCountScope)99 };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Scope));
    }

    [Fact]
    public void Validate_EmptyPreparedByEmployeeId_Fails()
    {
        var command = ValidCommand() with { PreparedByEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PreparedByEmployeeId));
    }

    [Fact]
    public void Validate_EmptyCertifiedByEmployeeId_Fails()
    {
        var command = ValidCommand() with { CertifiedByEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.CertifiedByEmployeeId));
    }

    [Fact]
    public void Validate_EmptyApprovedByEmployeeId_Fails()
    {
        var command = ValidCommand() with { ApprovedByEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ApprovedByEmployeeId));
    }

    private static CreatePhysicalCountSessionCommand ValidCommand() =>
        new(
            SessionNo: "PCS-2025-01-0001",
            CountDate: DateOnly.FromDateTime(DateTime.UtcNow),
            StationOffice: "Main Office",
            Scope: PhysicalCountScope.Both,
            PreparedByEmployeeId: Guid.NewGuid(),
            CertifiedByEmployeeId: Guid.NewGuid(),
            ApprovedByEmployeeId: Guid.NewGuid());
}

