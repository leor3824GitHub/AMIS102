using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.CreatePPEIRFormSeries;

public sealed class CreatePPEIRFormSeriesCommandValidator : AbstractValidator<CreatePPEIRFormSeriesCommand>
{
    public CreatePPEIRFormSeriesCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartSerial).GreaterThan(0);
        RuleFor(x => x.EndSerial).GreaterThanOrEqualTo(x => x.StartSerial);
    }
}
