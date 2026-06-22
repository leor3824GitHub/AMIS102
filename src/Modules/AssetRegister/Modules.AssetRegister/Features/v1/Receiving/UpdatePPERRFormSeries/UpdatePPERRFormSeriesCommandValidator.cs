using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.UpdatePPERRFormSeries;

public sealed class UpdatePPERRFormSeriesCommandValidator : AbstractValidator<UpdatePPERRFormSeriesCommand>
{
    public UpdatePPERRFormSeriesCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.StartSerial).GreaterThan(0);
        RuleFor(x => x.EndSerial).GreaterThanOrEqualTo(x => x.StartSerial);
    }
}
