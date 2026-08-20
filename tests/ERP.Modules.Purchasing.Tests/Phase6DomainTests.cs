// <copyright file="Phase6DomainTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.Purchasing.Tests;

public class OverReceiptToleranceTests
{
    private static PurchaseOrderLine CreatePoLine(decimal quantity)
    {
        return new PurchaseOrderLine(
            Guid.NewGuid(),
            1,
            "ITEM-001",
            "Test Item",
            quantity,
            "EA",
            10.00m,
            DateTime.UtcNow.AddDays(7),
            Guid.NewGuid(),
            null,
            null);
    }

    [Fact]
    public void UpdateQuantityReceivedWithinFivePercentToleranceShouldSucceed()
    {
        var poLine = CreatePoLine(100m);

        poLine.UpdateQuantityReceived(104m);

        poLine.QuantityReceived.Should().Be(104m);
    }

    [Fact]
    public void UpdateQuantityReceivedExactlyAtFivePercentShouldSucceed()
    {
        var poLine = CreatePoLine(100m);

        poLine.UpdateQuantityReceived(105m);

        poLine.QuantityReceived.Should().Be(105m);
    }

    [Fact]
    public void UpdateQuantityReceivedExceedsFivePercentToleranceShouldThrow()
    {
        var poLine = CreatePoLine(100m);

        var act = () => poLine.UpdateQuantityReceived(106m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceed over-receipt tolerance*");
    }

    [Fact]
    public void UpdateQuantityReceivedAccumulatedExceedsToleranceShouldThrow()
    {
        var poLine = CreatePoLine(100m);
        poLine.UpdateQuantityReceived(50m);

        var act = () => poLine.UpdateQuantityReceived(56m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceed over-receipt tolerance*");
    }

    [Fact]
    public void UpdateQuantityReceivedWithCustomToleranceShouldRespectCustomTolerance()
    {
        var poLine = CreatePoLine(100m);

        poLine.UpdateQuantityReceived(108m, overReceiptTolerance: 0.10m);

        poLine.QuantityReceived.Should().Be(108m);
    }

    [Fact]
    public void UpdateQuantityReceivedWithCustomToleranceExceedsShouldThrow()
    {
        var poLine = CreatePoLine(100m);

        var act = () => poLine.UpdateQuantityReceived(111m, overReceiptTolerance: 0.10m);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateQuantityReceivedZeroQuantityReceivedShouldSucceed()
    {
        var poLine = CreatePoLine(100m);

        poLine.UpdateQuantityReceived(0m);

        poLine.QuantityReceived.Should().Be(0m);
    }

    [Fact]
    public void UpdateQuantityReceivedNegativeQuantityShouldThrow()
    {
        var poLine = CreatePoLine(100m);

        var act = () => poLine.UpdateQuantityReceived(-10m);

        act.Should().Throw<ArgumentException>();
    }
}

public class ChangeOrderTests
{
    private static PurchaseOrder CreateApprovedPo()
    {
        var po = new PurchaseOrder(
            "PO-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            PurchaseOrderType.Standard,
            "Ship To",
            "123 Main St",
            null,
            Guid.NewGuid(),
            "Buyer notes",
            "Vendor ref",
            PurchaseOrderStatus.Draft);

        po.AddLine(new PurchaseOrderLine(
            po.Id,
            1,
            "ITEM-001",
            "Test Item",
            10m,
            "EA",
            50m,
            DateTime.UtcNow.AddDays(7),
            Guid.NewGuid(),
            null,
            null));
        po.SubmitForApproval();
        po.Approve(Guid.NewGuid());

        return po;
    }

    [Fact]
    public void CreateChangeOrderApprovedPoShouldIncrementRevisionAndReturnToDraft()
    {
        var po = CreateApprovedPo();

        po.CreateChangeOrder();

        po.RevisionNumber.Should().Be(1);
        po.Status.Should().Be(PurchaseOrderStatus.Draft);
    }

    [Fact]
    public void CreateChangeOrderDraftPoShouldThrow()
    {
        var po = new PurchaseOrder(
            "PO-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            PurchaseOrderType.Standard,
            "Ship To",
            "123 Main St",
            null,
            Guid.NewGuid(),
            "Buyer notes",
            "Vendor ref");

        po.AddLine(new PurchaseOrderLine(
            po.Id,
            1,
            "ITEM-001",
            "Test Item",
            10m,
            "EA",
            50m,
            DateTime.UtcNow.AddDays(7),
            Guid.NewGuid(),
            null,
            null));

        var act = () => po.CreateChangeOrder();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only approved POs*");
    }

    [Fact]
    public void CreateChangeOrderClosedPoShouldThrow()
    {
        var po = CreateApprovedPo();
        po.Close();

        var act = () => po.CreateChangeOrder();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only approved POs*");
    }
}

public class PurchaseOrderApprovalTests
{
    private static PurchaseOrder CreateDraftPo()
    {
        var po = new PurchaseOrder(
            "PO-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            PurchaseOrderType.Standard,
            "Ship To",
            "123 Main St",
            null,
            Guid.NewGuid(),
            "Buyer notes",
            "Vendor ref");

        po.AddLine(new PurchaseOrderLine(
            po.Id,
            1,
            "ITEM-001",
            "Test Item",
            10m,
            "EA",
            50m,
            DateTime.UtcNow.AddDays(7),
            Guid.NewGuid(),
            null,
            null));

        return po;
    }

    [Fact]
    public void SubmitForApprovalDraftWithLinesShouldSetPendingApproval()
    {
        var po = CreateDraftPo();

        po.SubmitForApproval();

        po.Status.Should().Be(PurchaseOrderStatus.PendingApproval);
    }

    [Fact]
    public void SubmitForApprovalNoLinesShouldThrow()
    {
        var po = new PurchaseOrder(
            "PO-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            PurchaseOrderType.Standard,
            "Ship To",
            "123 Main St",
            null,
            Guid.NewGuid(),
            "Buyer notes",
            "Vendor ref");

        var act = () => po.SubmitForApproval();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot submit PO with no lines*");
    }

    [Fact]
    public void ApprovePendingApprovalShouldSetApproved()
    {
        var po = CreateDraftPo();

        po.SubmitForApproval();
        po.Approve(Guid.NewGuid());

        po.Status.Should().Be(PurchaseOrderStatus.Approved);
        po.ApprovedDate.Should().NotBeNull();
    }
}

public class ReceiptWithoutPOTests
{
    private static ReceiptWithoutPO CreateReceipt()
    {
        return new ReceiptWithoutPO(
            "RCV-WOPO-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            "John Doe",
            "PS-001",
            "Test notes");
    }

    private static ReceiptWithoutPOLine CreateLine(Guid receiptId, int lineNumber, decimal qty, decimal price)
    {
        return new ReceiptWithoutPOLine(
            receiptId,
            lineNumber,
            "ITEM-001",
            "Test Item",
            qty,
            "EA",
            price,
            Guid.NewGuid(),
            null,
            null);
    }

    [Fact]
    public void CreateWithValidDataShouldSetDraftStatus()
    {
        var receipt = CreateReceipt();

        receipt.Status.Should().Be(ReceiptWithoutPOStatus.Draft);
        receipt.ReceiptNumber.Should().Be("RCV-WOPO-001");
        receipt.IsReversed.Should().BeFalse();
    }

    [Fact]
    public void AddLineDraftStatusShouldAddLine()
    {
        var receipt = CreateReceipt();

        receipt.AddLine(CreateLine(receipt.Id, 1, 10m, 50m));

        receipt.Lines.Should().HaveCount(1);
    }

    [Fact]
    public void AddLineNotDraftShouldThrow()
    {
        var receipt = CreateReceipt();
        receipt.AddLine(CreateLine(receipt.Id, 1, 10m, 50m));
        receipt.MarkPendingApproval(Guid.NewGuid());

        var act = () => receipt.AddLine(CreateLine(receipt.Id, 2, 5m, 25m));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot add lines*");
    }

    [Fact]
    public void MarkPendingApprovalWithLinesShouldSetPendingApproval()
    {
        var receipt = CreateReceipt();
        receipt.AddLine(CreateLine(receipt.Id, 1, 10m, 50m));

        var approvalRequestId = Guid.NewGuid();
        receipt.MarkPendingApproval(approvalRequestId);

        receipt.Status.Should().Be(ReceiptWithoutPOStatus.PendingApproval);
        receipt.ApprovalRequestId.Should().Be(approvalRequestId);
    }

    [Fact]
    public void MarkPendingApprovalWithNoLinesShouldThrow()
    {
        var receipt = CreateReceipt();

        var act = () => receipt.MarkPendingApproval(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot submit a receipt with no lines*");
    }

    [Fact]
    public void MarkPendingApprovalFromNonDraftShouldThrow()
    {
        var receipt = CreateReceipt();
        receipt.AddLine(CreateLine(receipt.Id, 1, 10m, 50m));
        receipt.MarkPendingApproval(Guid.NewGuid());

        var act = () => receipt.MarkPendingApproval(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot submit a receipt*");
    }

    [Fact]
    public void ApproveFromPendingApprovalShouldSetApproved()
    {
        var receipt = CreateReceipt();
        receipt.AddLine(CreateLine(receipt.Id, 1, 10m, 50m));
        receipt.MarkPendingApproval(Guid.NewGuid());

        receipt.Approve(Guid.NewGuid());

        receipt.Status.Should().Be(ReceiptWithoutPOStatus.Approved);
    }

    [Fact]
    public void ApproveFromDraftShouldThrow()
    {
        var receipt = CreateReceipt();

        var act = () => receipt.Approve(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot approve receipt*");
    }

    [Fact]
    public void PostFromApprovedShouldSetPosted()
    {
        var receipt = CreateReceipt();
        receipt.AddLine(CreateLine(receipt.Id, 1, 10m, 50m));
        receipt.MarkPendingApproval(Guid.NewGuid());
        receipt.Approve(Guid.NewGuid());

        receipt.Post();

        receipt.Status.Should().Be(ReceiptWithoutPOStatus.Posted);
        receipt.PostedDate.Should().NotBeNull();
    }

    [Fact]
    public void PostFromDraftShouldThrow()
    {
        var receipt = CreateReceipt();

        var act = () => receipt.Post();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot post receipt*");
    }

    [Fact]
    public void GetTotalAmountCalculatesCorrectSum()
    {
        var receipt = CreateReceipt();
        receipt.AddLine(CreateLine(receipt.Id, 1, 10m, 50m));
        receipt.AddLine(CreateLine(receipt.Id, 2, 5m, 25m));

        receipt.GetTotalAmount().Should().Be(625m);
    }

    [Fact]
    public void ReversePostedShouldSetReversed()
    {
        var receipt = CreateReceipt();
        receipt.AddLine(CreateLine(receipt.Id, 1, 10m, 50m));
        receipt.MarkPendingApproval(Guid.NewGuid());
        receipt.Approve(Guid.NewGuid());
        receipt.Post();

        receipt.Reverse("Test reversal");

        receipt.IsReversed.Should().BeTrue();
        receipt.ReversedDate.Should().NotBeNull();
        receipt.ReversalReason.Should().Be("Test reversal");
        receipt.Status.Should().Be(ReceiptWithoutPOStatus.Reversed);
    }

    [Fact]
    public void ReverseNotPostedShouldThrow()
    {
        var receipt = CreateReceipt();

        var act = () => receipt.Reverse("Test");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only posted receipts*");
    }

    [Fact]
    public void ReverseAlreadyReversedShouldThrow()
    {
        var receipt = CreateReceipt();
        receipt.AddLine(CreateLine(receipt.Id, 1, 10m, 50m));
        receipt.MarkPendingApproval(Guid.NewGuid());
        receipt.Approve(Guid.NewGuid());
        receipt.Post();
        receipt.Reverse("First reversal");

        var act = () => receipt.Reverse("Second reversal");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only posted receipts*");
    }

    [Fact]
    public void ReverseEmptyReasonShouldThrow()
    {
        var receipt = CreateReceipt();
        receipt.AddLine(CreateLine(receipt.Id, 1, 10m, 50m));
        receipt.MarkPendingApproval(Guid.NewGuid());
        receipt.Approve(Guid.NewGuid());
        receipt.Post();

        var act = () => receipt.Reverse(string.Empty);

        act.Should().Throw<ArgumentException>();
    }
}

public class OverReceiptApprovalTests
{
    private static OverReceiptApproval CreateApproval(decimal orderedQty, decimal receivedQty, decimal tolerance)
    {
        return new OverReceiptApproval(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "RCV-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            orderedQty,
            receivedQty,
            tolerance);
    }

    [Fact]
    public void CreateWithValidDataShouldSetPendingStatus()
    {
        var approval = CreateApproval(100m, 106m, 0.05m);

        approval.Status.Should().Be(OverReceiptApprovalStatus.Pending);
        approval.IsWithinTolerance.Should().BeFalse();
    }

    [Fact]
    public void IsWithinToleranceExactlyAtToleranceShouldBeTrue()
    {
        var approval = CreateApproval(100m, 105m, 0.05m);

        approval.IsWithinTolerance.Should().BeTrue();
    }

    [Fact]
    public void IsWithinToleranceWithinToleranceShouldBeTrue()
    {
        var approval = CreateApproval(100m, 103m, 0.05m);

        approval.IsWithinTolerance.Should().BeTrue();
    }

    [Fact]
    public void ResolveFromPendingShouldSetStatus()
    {
        var approval = CreateApproval(100m, 106m, 0.05m);

        approval.Resolve(OverReceiptApprovalStatus.Approved);

        approval.Status.Should().Be(OverReceiptApprovalStatus.Approved);
    }

    [Fact]
    public void ResolveFromNonPendingShouldThrow()
    {
        var approval = CreateApproval(100m, 106m, 0.05m);

        approval.Resolve(OverReceiptApprovalStatus.Approved);

        var act = () => approval.Resolve(OverReceiptApprovalStatus.Rejected);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot change status*");
    }
}
