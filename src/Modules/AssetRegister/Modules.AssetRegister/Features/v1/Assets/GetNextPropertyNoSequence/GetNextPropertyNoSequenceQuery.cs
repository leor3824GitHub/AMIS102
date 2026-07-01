using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetNextPropertyNoSequence;

/// <summary>
/// Previews the next available property-number sequence for a given year / office / class
/// prefix, without consuming it. Best-effort helper for the PropertyNo generator dialog
/// (manual entry aid). Ported from the retired AssetManagement tangible-items generator.
/// </summary>
public sealed record GetNextPropertyNoSequenceQuery(
    int Year,
    string OfficeCode,
    string ClassCode) : IQuery<NextPropertyNoSequenceResponse>;

public sealed record NextPropertyNoSequenceResponse(int NextSequence);
