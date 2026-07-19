using AMIS.Framework.Core.Context;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Domain.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.IssueJobOrder;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Integration;

/// <summary>
/// Guards the producer side of the inspector inspection-request notification: when a Job Order is Issued,
/// <see cref="IssueJobOrderCommandHandler"/> must publish a <see cref="NotificationRequestedIntegrationEvent"/>
/// to the assigned inspector — but only if that inspector has a linked login (<c>IdentityUserId</c>). The
/// publish is best-effort: a bus failure must never roll back the Issue.
/// </summary>
public sealed class IssueJobOrderNotificationTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Handle_PublishesInspectionRequestedEvent_WhenInspectorHasLinkedLogin()
    {
        using var ctx = new ProcurementTestContext(Tenant);
        var inspectorId = Guid.NewGuid();
        var jo = await SeedPendingApprovalJobOrderAsync(ctx.Db, inspectorId);

        const string inspectorLogin = "9f1b3c2d-0000-4444-8888-aaaaaaaaaaaa";
        var eventBus = Substitute.For<IEventBus>();
        NotificationRequestedIntegrationEvent? published = null;
        eventBus.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => published = ci.Arg<IIntegrationEvent>() as NotificationRequestedIntegrationEvent);

        var handler = NewHandler(ctx.Db, MediatorWithInspector(inspectorId, inspectorLogin), eventBus);

        var result = await handler.Handle(new IssueJobOrderCommand(jo.Id), CancellationToken.None);

        result.ShouldNotBeNull();
        jo.Status.ShouldBe(JobOrderStatus.Issued);

        await eventBus.Received(1).PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());
        published.ShouldNotBeNull();
        published!.RecipientUserId.ShouldBe(inspectorLogin);
        published.Type.ShouldBe(NotificationType.InspectionRequested);
        published.Source.ShouldBe("ProcurementAcquisition");
        published.CorrelationId.ShouldBe(jo.Id.ToString()); // idempotency key = JO id
        published.TenantId.ShouldBe(Tenant);
        published.Link.ShouldBe($"/procurement/job-orders?inspect={jo.Id}");
        published.Body.ShouldContain(jo.JoNumber);
    }

    [Fact]
    public async Task Handle_SkipsPublish_WhenInspectorHasNoLinkedLogin()
    {
        using var ctx = new ProcurementTestContext(Tenant);
        var inspectorId = Guid.NewGuid();
        var jo = await SeedPendingApprovalJobOrderAsync(ctx.Db, inspectorId);

        var eventBus = Substitute.For<IEventBus>();
        // Inspector is a directory-only employee with no identity account → nothing to notify.
        var handler = NewHandler(ctx.Db, MediatorWithInspector(inspectorId, identityUserId: null), eventBus);

        var result = await handler.Handle(new IssueJobOrderCommand(jo.Id), CancellationToken.None);

        // The Issue still succeeds; only the notification is skipped.
        result.ShouldNotBeNull();
        jo.Status.ShouldBe(JobOrderStatus.Issued);
        await eventBus.DidNotReceive().PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotThrow_AndStillIssues_WhenPublishFails()
    {
        using var ctx = new ProcurementTestContext(Tenant);
        var inspectorId = Guid.NewGuid();
        var jo = await SeedPendingApprovalJobOrderAsync(ctx.Db, inspectorId);

        var eventBus = Substitute.For<IEventBus>();
        eventBus.When(b => b.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("event bus unavailable"));

        var handler = NewHandler(
            ctx.Db, MediatorWithInspector(inspectorId, "9f1b3c2d-0000-4444-8888-aaaaaaaaaaaa"), eventBus);

        JobOrderDto? result = null;
        await Should.NotThrowAsync(async () =>
            result = await handler.Handle(new IssueJobOrderCommand(jo.Id), CancellationToken.None));

        result.ShouldNotBeNull();
        jo.Status.ShouldBe(JobOrderStatus.Issued); // notification failure must not roll back the Issue
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static IssueJobOrderCommandHandler NewHandler(
        AMIS.Modules.ProcurementAcquisition.Data.ProcurementDbContext db, IMediator mediator, IEventBus eventBus) =>
        new(db, CurrentUser(), mediator, eventBus, NullLogger<IssueJobOrderCommandHandler>.Instance);

    private static ICurrentUser CurrentUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated().Returns(true);
        user.GetUserId().Returns(Guid.NewGuid());
        user.Name.Returns("Issuing Official");
        return user;
    }

    /// <summary>
    /// Mediator answering both queries the handler issues: the signatory lookup (by identity user id) is left
    /// null so the issuer name falls back to the authenticated user; the inspector lookup (by employee id)
    /// returns an employee whose <c>IdentityUserId</c> drives the test case.
    /// </summary>
    private static IMediator MediatorWithInspector(Guid inspectorId, string? identityUserId)
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetEmployeeReferenceByIdentityUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((EmployeeReferenceDto?)null);
        mediator.Send(Arg.Is<GetEmployeeReferenceByIdQuery>(q => q.Id == inspectorId), Arg.Any<CancellationToken>())
            .Returns(Inspector(inspectorId, identityUserId));
        return mediator;
    }

    private static EmployeeReferenceDto Inspector(Guid id, string? identityUserId) => new(
        id, "EMP-INSP", identityUserId, "Ima", "Inspector", null,
        Guid.NewGuid(), "OFF", "Office", "Office Address", Guid.NewGuid(), "DEP", "Dept",
        Guid.NewGuid(), "POS", "Inspector", null, null, null, true);

    private static async Task<JobOrder> SeedPendingApprovalJobOrderAsync(
        AMIS.Modules.ProcurementAcquisition.Data.ProcurementDbContext db, Guid inspectorId)
    {
        var jo = JobOrder.Create(
            Tenant, "JO-NOTIF-1", purchaseRequestId: null, jobRequestNo: null, requisitioningOffice: null,
            supplierId: Guid.NewGuid(), supplierName: "ACME Works", supplierAddress: "addr", supplierTin: null,
            modeOfProcurement: ModeOfProcurement.SmallValueProcurement, placeOfDelivery: "HQ", dateOfDelivery: null,
            deliveryTerm: "30 days", paymentTerm: "On completion", fundCluster: null, oursBursNumber: null,
            inspectorId: inspectorId, inspectorName: "Inspector", inspectorDesignation: null,
            lineItems: [new JobOrderLineItemData(null, "Renovation", 1m, 10000m)]);

        // Walk the real workflow up to the point Issue is valid (PendingApproval); the handler performs Issue.
        jo.Submit();
        jo.CertifyFundsAvailable(Guid.NewGuid(), "Accountant", null, null, null);

        db.JobOrders.Add(jo);
        await db.SaveChangesAsync();
        return jo;
    }
}
