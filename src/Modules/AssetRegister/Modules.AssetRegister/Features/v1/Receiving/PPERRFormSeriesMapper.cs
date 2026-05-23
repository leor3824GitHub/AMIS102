using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Domain.Receiving;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving;

internal static class PPERRFormSeriesMapper
{
    public static PPERRFormSeriesDto ToDto(PPERRFormSeries s) =>
        new(s.Id, s.Label, s.StartSerial, s.EndSerial, s.NextSerial, s.Remaining, s.IsActive, s.IsExhausted, s.IsUnused);
}
