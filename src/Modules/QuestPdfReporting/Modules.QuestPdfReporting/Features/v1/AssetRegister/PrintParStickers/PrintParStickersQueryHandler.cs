using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertySticker;
using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintParStickers;

public sealed class PrintParStickersQueryHandler(IMediator mediator)
    : IQueryHandler<PrintParStickersQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintParStickersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await AccountabilityStickerComposer.BuildAsync(
            mediator, query.AccountabilityId, AccountabilityType.PPE_PAR,
            query.PaperSize, cancellationToken).ConfigureAwait(false);
    }
}
