using AMIS.Modules.AssetRegister.Contracts.v1.Incidents;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Incidents;

public sealed class RecordIncidentRecoveryCommandValidator
    : AbstractValidator<RecordIncidentRecoveryCommand>
{
    public RecordIncidentRecoveryCommandValidator()
    {
        RuleFor(x => x.IncidentReportId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.RecoveredOn).NotEqual(default(DateOnly));
    }
}

public sealed class RecordIncidentSettlementCommandValidator
    : AbstractValidator<RecordIncidentSettlementCommand>
{
    public RecordIncidentSettlementCommandValidator()
    {
        RuleFor(x => x.IncidentReportId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.SettledOn).NotEqual(default(DateOnly));
    }
}

public sealed class GrantIncidentReliefCommandValidator
    : AbstractValidator<GrantIncidentReliefCommand>
{
    public GrantIncidentReliefCommandValidator()
    {
        RuleFor(x => x.IncidentReportId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.GrantedOn).NotEqual(default(DateOnly));
        RuleFor(x => x.DecisionRef).NotEmpty().MaximumLength(200);
    }
}

public sealed class DerecognizeIncidentItemCommandValidator
    : AbstractValidator<DerecognizeIncidentItemCommand>
{
    public DerecognizeIncidentItemCommandValidator()
    {
        RuleFor(x => x.IncidentReportId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.RecordedOn).NotEqual(default(DateOnly));
    }
}

public sealed class CloseIncidentReportCommandValidator
    : AbstractValidator<CloseIncidentReportCommand>
{
    public CloseIncidentReportCommandValidator()
    {
        RuleFor(x => x.IncidentReportId).NotEmpty();
    }
}
