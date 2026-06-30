using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Repairs;

public sealed class RequestRepairCommandValidator : AbstractValidator<RequestRepairCommand>
{
    public RequestRepairCommandValidator()
    {
        RuleFor(x => x.AssetRegistryId).NotEmpty();
        RuleFor(x => x.NatureOfWork).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RequestedOn).NotEmpty();
        RuleFor(x => x.PartsToReplace).MaximumLength(2000).When(x => x.PartsToReplace is not null);
        RuleFor(x => x.EngineNo).MaximumLength(100).When(x => x.EngineNo is not null);
        RuleFor(x => x.ChassisNo).MaximumLength(100).When(x => x.ChassisNo is not null);
        RuleFor(x => x.NotedBy).MaximumLength(200).When(x => x.NotedBy is not null);
    }
}

public sealed class RecordPreRepairInspectionCommandValidator : AbstractValidator<RecordPreRepairInspectionCommand>
{
    public RecordPreRepairInspectionCommandValidator()
    {
        RuleFor(x => x.RepairId).NotEmpty();
        RuleFor(x => x.Findings).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PreInspectedBy).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PreInspectedOn).NotEmpty();
    }
}

public sealed class RecordPostRepairInspectionCommandValidator : AbstractValidator<RecordPostRepairInspectionCommand>
{
    public RecordPostRepairInspectionCommandValidator()
    {
        RuleFor(x => x.RepairId).NotEmpty();
        RuleFor(x => x.Findings).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PostInspectedBy).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PostInspectedOn).NotEmpty();
        RuleFor(x => x.AmountPerJO).GreaterThanOrEqualTo(0).When(x => x.AmountPerJO.HasValue);
        RuleFor(x => x.RepairShop).MaximumLength(300).When(x => x.RepairShop is not null);
    }
}

public sealed class AcceptRepairCommandValidator : AbstractValidator<AcceptRepairCommand>
{
    public AcceptRepairCommandValidator()
    {
        RuleFor(x => x.RepairId).NotEmpty();
        RuleFor(x => x.AcceptedBy).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AcceptedOn).NotEmpty();
    }
}
