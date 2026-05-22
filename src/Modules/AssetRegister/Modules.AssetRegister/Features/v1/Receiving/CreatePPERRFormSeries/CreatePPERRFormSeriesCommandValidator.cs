using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.CreatePPERRFormSeries;

public sealed class CreatePPERRFormSeriesCommandValidator : AbstractValidator<CreatePPERRFormSeriesCommand>
{
    public CreatePPERRFormSeriesCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartSerial).GreaterThan(0);
        RuleFor(x => x.EndSerial).GreaterThanOrEqualTo(x => x.StartSerial);
    }
}
