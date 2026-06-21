using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Domain.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.ApproveDisbursementVoucher;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.PayDisbursementVoucher;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace BudgetDisbursement.Tests.Handlers;

/// <summary>
/// Guards the second-pass fix: missing vouchers surface as a framework <see cref="NotFoundException"/>
/// (HTTP 404) and invalid state transitions as a <see cref="CustomException"/> (HTTP 400) carrying the
/// domain message — not the bare BCL exceptions that the global handler renders as a generic 500.
/// </summary>
public sealed class DisbursementVoucherMutationErrorTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Approve_MissingVoucher_ThrowsNotFound()
    {
        using var ctx = new BudgetDisbursementTestContext(Tenant);
        var handler = new ApproveDisbursementVoucherCommandHandler(
            NullLogger<ApproveDisbursementVoucherCommandHandler>.Instance, ctx.Db);

        var act = async () => await handler.Handle(new ApproveDisbursementVoucherCommand(Guid.NewGuid()), CancellationToken.None);

        await act.ShouldThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Pay_DraftVoucher_ThrowsBadRequest()
    {
        using var ctx = new BudgetDisbursementTestContext(Tenant);
        var dv = SeedDraft();
        ctx.Db.DisbursementVouchers.Add(dv);
        await ctx.Db.SaveChangesAsync();

        var handler = new PayDisbursementVoucherCommandHandler(
            NullLogger<PayDisbursementVoucherCommandHandler>.Instance, ctx.Db);

        var act = async () => await handler.Handle(
            new PayDisbursementVoucherCommand(dv.Id, DateOnly.FromDateTime(DateTime.UtcNow), null), CancellationToken.None);

        // Only Approved DVs can be paid → 400, not a 500.
        var ex = await act.ShouldThrowAsync<CustomException>();
        ex.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    private static DisbursementVoucher SeedDraft() =>
        DisbursementVoucher.Create(
            dvNumber: "DV-2026-00001",
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderNumber: "PO-2026-001",
            budgetUtilizationRequestId: Guid.NewGuid(),
            burNumber: "BUR-2026-00001",
            dvDate: DateOnly.FromDateTime(DateTime.UtcNow),
            fundCluster: "01",
            payee: "Acme Supplies Inc.",
            tinNo: null,
            payeeAddress: null,
            particulars: "Payment for PO-2026-001",
            amount: 5000m,
            modeOfPayment: "Check",
            remarks: null);
}
