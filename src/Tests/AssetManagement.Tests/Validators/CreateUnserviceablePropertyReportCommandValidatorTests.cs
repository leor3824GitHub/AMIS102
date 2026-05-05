using FSH.Modules.AssetManagement.Domain;
using FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.CreateUnserviceablePropertyReport;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Validators;

public sealed class CreateUnserviceablePropertyReportCommandValidatorTests
{
    private readonly CreateUnserviceablePropertyReportCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = ValidCommand();

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyReportNo_Fails()
    {
        var command = ValidCommand() with { ReportNo = string.Empty };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReportNo));
    }

    [Theory]
    [InlineData("IUR-2024-01-0001")]
    [InlineData("IUR-2025-12-9999")]
    public void Validate_ValidReportNoFormat_Passes(string reportNo)
    {
        var command = ValidCommand() with { ReportNo = reportNo };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("IUR2024010001")]
    [InlineData("IUR-24-01-0001")]
    [InlineData("ABC-2024-01-0001")]
    [InlineData("iur-2024-01-0001")]
    public void Validate_InvalidReportNoFormat_Fails(string reportNo)
    {
        var command = ValidCommand() with { ReportNo = reportNo };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReportNo));
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
            Items = [new CreateUnserviceablePropertyItemRequest(Guid.Empty, null)]
        };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    private static CreateUnserviceablePropertyReportCommand ValidCommand() =>
        new(
            ReportNo: "IUR-2025-01-0001",
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            DisposalMethod: DisposalMethod.Sale,
            FundCluster: "101",
            InspectedByEmployeeId: Guid.NewGuid(),
            ApprovedByEmployeeId: Guid.NewGuid(),
            Remarks: null,
            Items: [new CreateUnserviceablePropertyItemRequest(Guid.NewGuid(), "Broken screen")]);
}
