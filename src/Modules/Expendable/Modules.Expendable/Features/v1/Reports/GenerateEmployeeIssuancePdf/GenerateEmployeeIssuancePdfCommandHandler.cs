using AMIS.Modules.Expendable.Contracts.v1.Reports;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Domain.Requests;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GenerateEmployeeIssuancePdf;

public sealed class GenerateEmployeeIssuancePdfCommandHandler(
    ExpendableDbContext db,
    IMediator mediator)
    : ICommandHandler<GenerateEmployeeIssuancePdfCommand, byte[]>
{
    public async ValueTask<byte[]> Handle(
        GenerateEmployeeIssuancePdfCommand command, CancellationToken cancellationToken)
    {
        // 1. Fetch all matching fulfilled requests (mirrors existing query handler logic)
        var q = db.SupplyRequests
            .AsNoTracking()
            .Where(r => r.Status == SupplyRequestStatus.Fulfilled);

        if (!string.IsNullOrWhiteSpace(command.EmployeeId))
            q = q.Where(r => r.EmployeeId == command.EmployeeId);
        if (command.From.HasValue)
            q = q.Where(r => r.LastModifiedOnUtc >= command.From.Value);
        if (command.To.HasValue)
            q = q.Where(r => r.LastModifiedOnUtc <= command.To.Value);

        var requests = await q
            .OrderByDescending(r => r.LastModifiedOnUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var productIds = requests.SelectMany(r => r.Items).Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.SKU })
            .ToDictionaryAsync(p => p.Id, cancellationToken)
            .ConfigureAwait(false);

        var records = requests.Select(r => new EmployeeIssuanceDto(
            r.Id,
            r.RequestNumber,
            r.EmployeeId,
            r.DepartmentId,
            r.LastModifiedOnUtc ?? r.CreatedOnUtc,
            r.Items.Where(i => i.FulfilledQuantity > 0).Select(i =>
            {
                var p = products.GetValueOrDefault(i.ProductId);
                return new IssuanceItemDto(
                    i.ProductId,
                    p?.Name ?? "Unknown",
                    p?.SKU ?? string.Empty,
                    i.FulfilledQuantity,
                    i.FulfilledQuantity > 0 ? Math.Round(i.FulfilledValue / i.FulfilledQuantity, 4) : 0m,
                    i.FulfilledValue);
            }).ToList(),
            r.Items.Sum(i => i.FulfilledValue)
        )).ToList();

        // 2. Fetch org profile
        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken)
            .ConfigureAwait(false);

        // 3. Build name lookups from the data we have (IDs only — no cross-module DB access)
        var employeeNames = records.Select(r => r.EmployeeId).Distinct()
            .ToDictionary(id => id, id => id);
        var deptNames = records.Select(r => r.DepartmentId).Distinct()
            .ToDictionary(id => id, id => id);

        return new EmployeeIssuancePdfDocument(
            records, org, command.From, command.To, employeeNames, deptNames)
            .GeneratePdf();
    }
}

