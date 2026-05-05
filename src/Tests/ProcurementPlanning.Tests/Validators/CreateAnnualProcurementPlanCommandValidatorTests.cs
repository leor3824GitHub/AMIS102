using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateAnnualProcurementPlan;
using Shouldly;
using Xunit;

namespace ProcurementPlanning.Tests.Validators;

public sealed class CreateAnnualProcurementPlanCommandValidatorTests
{
    private readonly CreateAnnualProcurementPlanCommandValidator _sut = new();

    [Theory]
    [InlineData(2000)]
    [InlineData(2025)]
    [InlineData(2100)]
    public void Validate_ValidFiscalYear_Passes(int year)
    {
        var command = new CreateAnnualProcurementPlanCommand(year, AppPhase.Indicative);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    [InlineData(0)]
    public void Validate_FiscalYearOutOfRange_Fails(int year)
    {
        var command = new CreateAnnualProcurementPlanCommand(year, AppPhase.Indicative);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.FiscalYear));
    }
}
