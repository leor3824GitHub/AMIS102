using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Finance.PrintBudgetUtilizationRecord;

public sealed class PrintBudgetUtilizationRecordQueryHandler(IMediator mediator)
    : IQueryHandler<PrintBudgetUtilizationRecordQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintBudgetUtilizationRecordQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var bur = await mediator.Send(new GetBudgetUtilizationRecordByIdQuery(query.BurId), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Budget utilization record '{query.BurId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new BudgetUtilizationRecordPdfDocument(
            bur, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
