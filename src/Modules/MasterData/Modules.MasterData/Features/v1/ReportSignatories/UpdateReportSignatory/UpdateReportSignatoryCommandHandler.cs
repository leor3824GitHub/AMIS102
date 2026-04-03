using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.ReportSignatories;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.ReportSignatories.UpdateReportSignatory;

public sealed class UpdateReportSignatoryCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdateReportSignatoryCommand, ReportSignatoryDto>
{
    public async ValueTask<ReportSignatoryDto> Handle(
        UpdateReportSignatoryCommand command, CancellationToken cancellationToken)
    {
        var entity = await db.ReportSignatories
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Report signatory {command.Id} not found.");

        var orderTaken = await db.ReportSignatories
            .AnyAsync(x => x.Id != command.Id && x.ReportType == command.ReportType && x.SortOrder == command.SortOrder, cancellationToken)
            .ConfigureAwait(false);

        if (orderTaken)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(
                    nameof(command.SortOrder),
                    $"Sort order {command.SortOrder} is already in use for report type '{command.ReportType}'.")
            ]);
        }

        entity.Update(command.ReportType, command.SortOrder, command.Label, command.Name, command.Title, command.IsActive);
        entity.LastModifiedBy = currentUser.GetUserId().ToString();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ReportSignatoryDto(entity.Id, entity.ReportType, entity.SortOrder, entity.Label, entity.Name, entity.Title, entity.IsActive);
    }
}
