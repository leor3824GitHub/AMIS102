using AMIS.Modules.AssetRegister.Contracts.v1.Unserviceable;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Unserviceable;

public sealed class CreateUnserviceableReportDraftCommandValidator
    : AbstractValidator<CreateUnserviceableReportDraftCommand>
{
    public CreateUnserviceableReportDraftCommandValidator()
    {
        RuleFor(x => x.ReportType).IsInEnum();
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Station).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AsAt).NotEqual(default(DateOnly));
        RuleFor(x => x.AccountableOfficer).NotNull();
        RuleFor(x => x.AccountableOfficer.PrintedName)
            .NotEmpty()
            .When(x => x.AccountableOfficer is not null);
    }
}

public sealed class AddUnserviceableReportItemCommandValidator
    : AbstractValidator<AddUnserviceableReportItemCommand>
{
    public AddUnserviceableReportItemCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.AssetRegistryId).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

public sealed class SubmitUnserviceableReportCommandValidator
    : AbstractValidator<SubmitUnserviceableReportCommand>
{
    public SubmitUnserviceableReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.ApprovedBy).NotNull();
        RuleFor(x => x.ApprovedBy.PrintedName)
            .NotEmpty()
            .When(x => x.ApprovedBy is not null);
    }
}

public sealed class RecordUnserviceableInspectionCommandValidator
    : AbstractValidator<RecordUnserviceableInspectionCommand>
{
    public RecordUnserviceableInspectionCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.InspectedBy).NotNull();
        RuleFor(x => x.InspectedOn).NotEqual(default(DateOnly));
        RuleFor(x => x.Decisions)
            .NotNull()
            .Must(d => d is not null && d.Count > 0)
            .WithMessage("At least one inspection decision is required.");
        RuleForEach(x => x.Decisions).ChildRules(d =>
        {
            d.RuleFor(i => i.ItemId).NotEmpty();
            d.RuleFor(i => i.Method).IsInEnum();
            d.RuleFor(i => i.OtherSpecify).MaximumLength(500);
            d.RuleFor(i => i.AppraisedValue)
                .GreaterThanOrEqualTo(0)
                .When(i => i.AppraisedValue.HasValue);
        });
    }
}

public sealed class RecordUnserviceableDisposalCommandValidator
    : AbstractValidator<RecordUnserviceableDisposalCommand>
{
    public RecordUnserviceableDisposalCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Records)
            .NotNull()
            .Must(r => r is not null && r.Count > 0)
            .WithMessage("At least one disposal record is required.");
        RuleForEach(x => x.Records).ChildRules(r =>
        {
            r.RuleFor(i => i.ItemId).NotEmpty();
            r.RuleFor(i => i.DisposalRecordedOn).NotEqual(default(DateOnly));
            r.RuleFor(i => i.SaleORNo).MaximumLength(64);
            r.RuleFor(i => i.SaleAmount)
                .GreaterThanOrEqualTo(0)
                .When(i => i.SaleAmount.HasValue);
        });
    }
}

public sealed class CloseUnserviceableReportCommandValidator
    : AbstractValidator<CloseUnserviceableReportCommand>
{
    public CloseUnserviceableReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
    }
}
