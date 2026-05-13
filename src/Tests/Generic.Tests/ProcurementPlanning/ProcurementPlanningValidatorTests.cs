using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Domain.Ppmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateAnnualProcurementPlan;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateUpdateApp;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ReturnAnnualProcurementPlan;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.CreatePpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.CreateUpdatePpmp;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ReturnPpmp;

namespace Generic.Tests.ProcurementPlanning;

public sealed class ProcurementPlanningValidatorTests
{
    // ── CreatePpmpCommandValidator ─────────────────────────────────────────────

    [Fact]
    public void CreatePpmp_ValidCommand_PassesValidation()
    {
        var validator = new CreatePpmpCommandValidator();
        var command = new CreatePpmpCommand(2027, PpmpPhase.Indicative, "ICT", "ICT Unit",
            Guid.NewGuid(), [ValidItem()]);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void CreatePpmp_FiscalYearOutOfRange_Fails()
    {
        var validator = new CreatePpmpCommandValidator();
        var command = new CreatePpmpCommand(1999, PpmpPhase.Indicative, "ICT", "ICT Unit",
            Guid.NewGuid(), [ValidItem()]);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "FiscalYear");
    }

    [Fact]
    public void CreatePpmp_EmptyOfficeCode_Fails()
    {
        var validator = new CreatePpmpCommandValidator();
        var command = new CreatePpmpCommand(2027, PpmpPhase.Indicative, "", "ICT Unit",
            Guid.NewGuid(), [ValidItem()]);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "OfficeCode");
    }

    [Fact]
    public void CreatePpmp_EmptyEndUserUnit_Fails()
    {
        var validator = new CreatePpmpCommandValidator();
        var command = new CreatePpmpCommand(2027, PpmpPhase.Indicative, "ICT", "",
            Guid.NewGuid(), [ValidItem()]);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "EndUserUnit");
    }

    [Fact]
    public void CreatePpmp_EmptyPreparedById_Fails()
    {
        var validator = new CreatePpmpCommandValidator();
        var command = new CreatePpmpCommand(2027, PpmpPhase.Indicative, "ICT", "ICT Unit",
            Guid.Empty, [ValidItem()]);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "PreparedById");
    }

    [Fact]
    public void CreatePpmp_NoItems_Fails()
    {
        var validator = new CreatePpmpCommandValidator();
        var command = new CreatePpmpCommand(2027, PpmpPhase.Indicative, "ICT", "ICT Unit",
            Guid.NewGuid(), []);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Items");
    }

    [Theory]
    [InlineData("")]
    [InlineData("2027-01")]   // wrong format
    [InlineData("Jan 2027")]
    public void CreatePpmp_InvalidDateFormat_Fails(string badDate)
    {
        var validator = new CreatePpmpCommandValidator();
        var item = new PpmpItemRequest("Laptop", ProjectType.Goods, 1, "lot", "Shopping",
            false, badDate, "03/2027", "04/2027", "General Fund", 10_000m, null, null);
        var command = new CreatePpmpCommand(2027, PpmpPhase.Indicative, "ICT", "ICT Unit",
            Guid.NewGuid(), [item]);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreatePpmp_ZeroQuantity_Fails()
    {
        var validator = new CreatePpmpCommandValidator();
        var item = new PpmpItemRequest("Laptop", ProjectType.Goods, 0, "lot", "Shopping",
            false, "01/2027", "03/2027", "04/2027", "General Fund", 10_000m, null, null);
        var command = new CreatePpmpCommand(2027, PpmpPhase.Indicative, "ICT", "ICT Unit",
            Guid.NewGuid(), [item]);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public void CreatePpmp_ZeroBudget_Fails()
    {
        var validator = new CreatePpmpCommandValidator();
        var item = new PpmpItemRequest("Laptop", ProjectType.Goods, 1, "lot", "Shopping",
            false, "01/2027", "03/2027", "04/2027", "General Fund", 0m, null, null);
        var command = new CreatePpmpCommand(2027, PpmpPhase.Indicative, "ICT", "ICT Unit",
            Guid.NewGuid(), [item]);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("EstimatedBudget"));
    }

    // ── CreateUpdatePpmpCommandValidator ──────────────────────────────────────

    [Fact]
    public void CreateUpdatePpmp_ValidCommand_Passes()
    {
        var result = new CreateUpdatePpmpCommandValidator()
            .Validate(new CreateUpdatePpmpCommand(Guid.NewGuid(), "Budget revision"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void CreateUpdatePpmp_EmptyId_Fails()
    {
        var result = new CreateUpdatePpmpCommandValidator()
            .Validate(new CreateUpdatePpmpCommand(Guid.Empty, "Reason"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void CreateUpdatePpmp_EmptyUpdateReason_Fails()
    {
        var result = new CreateUpdatePpmpCommandValidator()
            .Validate(new CreateUpdatePpmpCommand(Guid.NewGuid(), ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "UpdateReason");
    }

    [Fact]
    public void CreateUpdatePpmp_UpdateReasonTooLong_Fails()
    {
        var result = new CreateUpdatePpmpCommandValidator()
            .Validate(new CreateUpdatePpmpCommand(Guid.NewGuid(), new string('x', 1001)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "UpdateReason");
    }

    // ── ReturnPpmpCommandValidator ─────────────────────────────────────────────

    [Fact]
    public void ReturnPpmp_ValidCommand_Passes()
    {
        var result = new ReturnPpmpCommandValidator()
            .Validate(new ReturnPpmpCommand(Guid.NewGuid(), "Needs correction"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ReturnPpmp_EmptyReturnReason_Fails()
    {
        var result = new ReturnPpmpCommandValidator()
            .Validate(new ReturnPpmpCommand(Guid.NewGuid(), ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "ReturnReason");
    }

    // ── CreateAnnualProcurementPlanCommandValidator ───────────────────────────

    [Theory]
    [InlineData(2027)]
    [InlineData(2000)]
    [InlineData(2100)]
    public void CreateApp_ValidFiscalYear_Passes(int year)
    {
        var result = new CreateAnnualProcurementPlanCommandValidator()
            .Validate(new CreateAnnualProcurementPlanCommand(year, AppPhase.Indicative));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void CreateApp_FiscalYearOutOfRange_Fails(int year)
    {
        var result = new CreateAnnualProcurementPlanCommandValidator()
            .Validate(new CreateAnnualProcurementPlanCommand(year, AppPhase.Indicative));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "FiscalYear");
    }

    // ── CreateUpdateAppCommandValidator ───────────────────────────────────────

    [Fact]
    public void CreateUpdateApp_ValidCommand_Passes()
    {
        var result = new CreateUpdateAppCommandValidator()
            .Validate(new CreateUpdateAppCommand(Guid.NewGuid(), "Supplemental budget"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void CreateUpdateApp_EmptyId_Fails()
    {
        var result = new CreateUpdateAppCommandValidator()
            .Validate(new CreateUpdateAppCommand(Guid.Empty, "Reason"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void CreateUpdateApp_EmptyUpdateReason_Fails()
    {
        var result = new CreateUpdateAppCommandValidator()
            .Validate(new CreateUpdateAppCommand(Guid.NewGuid(), ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "UpdateReason");
    }

    [Fact]
    public void CreateUpdateApp_UpdateReasonTooLong_Fails()
    {
        var result = new CreateUpdateAppCommandValidator()
            .Validate(new CreateUpdateAppCommand(Guid.NewGuid(), new string('x', 1001)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "UpdateReason");
    }

    // ── ReturnAnnualProcurementPlanCommandValidator ───────────────────────────

    [Fact]
    public void ReturnApp_ValidCommand_Passes()
    {
        var result = new ReturnAnnualProcurementPlanCommandValidator()
            .Validate(new ReturnAppCommand(Guid.NewGuid(), "Incomplete items"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ReturnApp_EmptyReturnReason_Fails()
    {
        var result = new ReturnAnnualProcurementPlanCommandValidator()
            .Validate(new ReturnAppCommand(Guid.NewGuid(), ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "ReturnReason");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PpmpItemRequest ValidItem() =>
        new("Laptop", ProjectType.Goods, 1, "lot", "Shopping",
            false, "01/2027", "03/2027", "04/2027", "General Fund", 80_000m, null, null);
}

