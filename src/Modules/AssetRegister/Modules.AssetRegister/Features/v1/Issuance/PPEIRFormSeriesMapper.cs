using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Domain.Issuance;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance;

internal static class PPEIRFormSeriesMapper
{
    public static PPEIRFormSeriesDto ToDto(PPEIRFormSeries s) =>
        new(s.Id, s.StartSerial, s.EndSerial, s.NextSerial, s.Remaining, s.IsActive, s.IsExhausted, s.IsUnused);
}
