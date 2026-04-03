using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.ReportSignatories;
using FSH.Modules.MasterData.Data;
using FSH.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.ReportSignatories.CreateReportSignatory;

public sealed class CreateReportSignatoryCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<CreateReportSignatoryCommand, ReportSignatoryDto>
{
    public async ValueTask<ReportSignatoryDto> Handle(
        CreateReportSignatoryCommand command, CancellationToken cancellationToken)
    {
        var orderTaken = await db.ReportSignatories
            .AnyAsync(x => x.ReportType == command.ReportType && x.SortOrder == command.SortOrder, cancellationToken)
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

        var tenantId = currentUser.GetTenant() ?? string.Empty;
        var entity = ReportSignatory.Create(tenantId, command.ReportType, command.SortOrder, command.Label, command.Name, command.Title);
        entity.CreatedBy = currentUser.GetUserId().ToString();

        db.ReportSignatories.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ReportSignatoryDto(entity.Id, entity.ReportType, entity.SortOrder, entity.Label, entity.Name, entity.Title, entity.IsActive);
    }
}
