using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertySticker;
using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintIcsStickers;

public sealed class PrintIcsStickersQueryHandler(IMediator mediator)
    : IQueryHandler<PrintIcsStickersQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintIcsStickersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await AccountabilityStickerComposer.BuildAsync(
            mediator, query.AccountabilityId, AccountabilityType.SE_ICS,
            query.PaperSize, cancellationToken).ConfigureAwait(false);
    }
}
