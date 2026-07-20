using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Inspections;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Domain.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Inspections.GetMyPendingInspections;
using Mediator;
using NSubstitute;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Integration;

/// <summary>
/// Guards the unified inspection worklist's Procurement producer: it must surface only the <b>calling</b>
/// inspector's Issued Job Orders and PendingInspection IARs, mapped to the shared
/// <see cref="AMIS.Framework.Shared.Inspections.PendingInspectionItem"/> shape with correct deep-link routes.
/// </summary>
public sealed class GetMyPendingProcurementInspectionsTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Handle_ReturnsOnlyMyPendingJobOrdersAndIars_WithCorrectRoutes()
    {
        using var ctx = new ProcurementTestContext(Tenant);
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();

        await SeedJobOrderAsync(ctx.Db, "JO-MINE", inspectorId: me, issue: true);     // ✅ mine + Issued
        await SeedJobOrderAsync(ctx.Db, "JO-DRAFT", inspectorId: me, issue: false);    // ✗ mine but Draft
        await SeedJobOrderAsync(ctx.Db, "JO-OTHER", inspectorId: other, issue: true);  // ✗ Issued but not mine
        await SeedIarAsync(ctx.Db, "IAR-MINE", inspectedById: me, submit: true);       // ✅ mine + PendingInspection
        await SeedIarAsync(ctx.Db, "IAR-DRAFT", inspectedById: me, submit: false);     // ✗ mine but Draft
        await SeedIarAsync(ctx.Db, "IAR-OTHER", inspectedById: other, submit: true);   // ✗ Pending but not mine

        var handler = new GetMyPendingProcurementInspectionsQueryHandler(ctx.Db, CurrentUser(), MediatorReturning(me));
        var result = await handler.Handle(new GetMyPendingProcurementInspectionsQuery(), CancellationToken.None);

        result.Count.ShouldBe(2);

        var jo = result.Single(i => i.SourceType == "JobOrder");
        jo.Reference.ShouldBe("JO-MINE");
        jo.ActionRoute.ShouldBe($"/procurement/job-orders?inspect={jo.SourceId}");

        var iar = result.Single(i => i.SourceType == "IAR");
        iar.Reference.ShouldBe("IAR-MINE");
        iar.ActionRoute.ShouldBe($"/procurement/inspection-acceptance-reports/{iar.SourceId}/inspect");
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenCallerHasNoLinkedEmployee()
    {
        using var ctx = new ProcurementTestContext(Tenant);
        await SeedJobOrderAsync(ctx.Db, "JO-1", inspectorId: Guid.NewGuid(), issue: true);

        // Mediator returns null → user has no employee profile.
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetEmployeeReferenceByIdentityUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((EmployeeReferenceDto?)null);

        var handler = new GetMyPendingProcurementInspectionsQueryHandler(ctx.Db, CurrentUser(), mediator);
        var result = await handler.Handle(new GetMyPendingProcurementInspectionsQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    private static ICurrentUser CurrentUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated().Returns(true);
        user.GetUserId().Returns(Guid.NewGuid());
        return user;
    }

    private static IMediator MediatorReturning(Guid employeeId)
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetEmployeeReferenceByIdentityUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Employee(employeeId));
        return mediator;
    }

    private static EmployeeReferenceDto Employee(Guid id) => new(
        id, "EMP-1", null, "Test", "User", null,
        Guid.NewGuid(), "OFF", "Office", "Office Address", Guid.NewGuid(), "DEP", "Dept",
        Guid.NewGuid(), "POS", "Position", true);

    private static async Task SeedJobOrderAsync(ProcurementDbContext db, string joNumber, Guid inspectorId, bool issue)
    {
        var jo = JobOrder.Create(
            Tenant, joNumber, purchaseRequestId: null, jobRequestNo: null, requisitioningOffice: null,
            supplierId: Guid.NewGuid(), supplierName: "ACME Works", supplierAddress: "addr", supplierTin: null,
            modeOfProcurement: ModeOfProcurement.SmallValueProcurement, placeOfDelivery: "HQ", dateOfDelivery: null,
            deliveryTerm: "30 days", paymentTerm: "On completion", fundCluster: null, oursBursNumber: null,
            inspectorId: inspectorId, inspectorName: "Inspector", inspectorDesignation: null,
            lineItems: [new JobOrderLineItemData(null, "Renovation", 1m, 10000m)]);

        if (issue)
        {
            jo.Submit();
            jo.CertifyFundsAvailable(Guid.NewGuid(), "Accountant", null, null, null);
            jo.Issue(Guid.NewGuid(), "Issuer");
        }

        db.JobOrders.Add(jo);
        await db.SaveChangesAsync();
    }

    private static async Task SeedIarAsync(ProcurementDbContext db, string iarNumber, Guid inspectedById, bool submit)
    {
        var lineItem = new InspectionAcceptanceReportLineItemRequest(
            Description: "Steel Cabinet", TechnicalSpecifications: null, Brand: null, Model: null,
            SerialNo: null, PropertyClassHint: null, Unit: "unit", Quantity: 1m, UnitCost: 5000m,
            InspectionRemarks: null);

        var iar = InspectionAcceptanceReport.Create(
            Tenant, iarNumber, purchaseOrderId: Guid.NewGuid(), supplierId: Guid.NewGuid(),
            supplierName: "ACME Supplies", inspectedById: inspectedById, receivedById: Guid.NewGuid(),
            deliveryReceiptNo: null, deliveryDate: null, remarks: null,
            lineItems: [lineItem], category: ProcurementCategory.Asset);

        if (submit)
            iar.SubmitForInspection();

        db.InspectionAcceptanceReports.Add(iar);
        await db.SaveChangesAsync();
    }
}
