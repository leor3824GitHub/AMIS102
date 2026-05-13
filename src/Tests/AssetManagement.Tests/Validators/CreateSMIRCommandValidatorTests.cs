using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.CreateSMIR;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Validators;

public sealed class CreateSMIRCommandValidatorTests
{
    private readonly CreateSMIRCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptySMIRNo_Fails()
    {
        var command = ValidCommand() with { SMIRNo = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.SMIRNo));
    }

    [Fact]
    public void Validate_SMIRNoExceeds32Characters_Fails()
    {
        var command = ValidCommand() with { SMIRNo = new string('S', 33) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.SMIRNo));
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
    public void Validate_InvalidIssuanceType_Fails()
    {
        var command = ValidCommand() with { IssuanceType = (SMIRIssuanceType)99 };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.IssuanceType));
    }

    [Theory]
    [InlineData(SMIRIssuanceType.Donation)]
    [InlineData(SMIRIssuanceType.Disposal)]
    [InlineData(SMIRIssuanceType.Sale)]
    [InlineData(SMIRIssuanceType.Others)]
    public void Validate_NonTransferIssuanceTypeWithoutTenantId_Passes(SMIRIssuanceType issuanceType)
    {
        var command = ValidCommand() with
        {
            IssuanceType = issuanceType,
            TransferredToTenantId = null
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_TransferIssuanceTypeWithTenantId_Passes()
    {
        var command = ValidCommand() with
        {
            IssuanceType = SMIRIssuanceType.Transfer,
            TransferredToTenantId = "tenant-abc-123"
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_TransferIssuanceTypeWithEmptyTenantId_Fails()
    {
        var command = ValidCommand() with
        {
            IssuanceType = SMIRIssuanceType.Transfer,
            TransferredToTenantId = string.Empty
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.TransferredToTenantId));
    }

    [Fact]
    public void Validate_TransferIssuanceTypeWithNullTenantId_Fails()
    {
        var command = ValidCommand() with
        {
            IssuanceType = SMIRIssuanceType.Transfer,
            TransferredToTenantId = null
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.TransferredToTenantId));
    }

    [Fact]
    public void Validate_TransferIssuanceTypeTenantIdExceeds64Characters_Fails()
    {
        var command = ValidCommand() with
        {
            IssuanceType = SMIRIssuanceType.Transfer,
            TransferredToTenantId = new string('T', 65)
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.TransferredToTenantId));
    }

    [Fact]
    public void Validate_TransferredToOfficerNameExceeds200Characters_Fails()
    {
        var command = ValidCommand() with { TransferredToOfficerName = new string('N', 201) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.TransferredToOfficerName));
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
            Items = [new CreateSMIRItemRequest(Guid.Empty, null)]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("TangibleInventoryItemId"));
    }

    [Fact]
    public void Validate_ItemDescriptionExceeds500Characters_Fails()
    {
        var command = ValidCommand() with
        {
            Items = [new CreateSMIRItemRequest(Guid.NewGuid(), new string('D', 501))]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("Description"));
    }

    private static CreateSMIRCommand ValidCommand() =>
        new(
            SMIRNo: "SMIR-2025-01-0001",
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            FundCluster: "101",
            IssuanceType: SMIRIssuanceType.Donation,
            TransferredToTenantId: null,
            TransferredToOfficerName: null,
            IssuedByEmployeeId: Guid.NewGuid(),
            Remarks: null,
            Items: [new CreateSMIRItemRequest(Guid.NewGuid(), "Laptop")]);
}

