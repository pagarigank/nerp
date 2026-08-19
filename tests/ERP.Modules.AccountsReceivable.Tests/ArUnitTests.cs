using ERP.Core.Common;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.AccountsReceivable.Tests;

public class ArUnitTests
{
    [Fact]
    public void AgingBucketCalculationReturnsCorrectBuckets()
    {
        var now = DateTimeOffset.UtcNow;
        var customerId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var currentInvoice = new Invoice(batchId, customerId, "INV-001", now, now.AddDays(10), null, null, null, null);
        currentInvoice.AddLine(Guid.NewGuid(), "Current", 1, 100, 0, 0);
        var currentDue = currentInvoice.BalanceDue;

        var aged30 = new Invoice(batchId, customerId, "INV-002", now.AddDays(-40), now.AddDays(-30), null, null, null, null);
        aged30.AddLine(Guid.NewGuid(), "30 Days", 1, 200, 0, 0);

        var aged60 = new Invoice(batchId, customerId, "INV-003", now.AddDays(-70), now.AddDays(-60), null, null, null, null);
        aged60.AddLine(Guid.NewGuid(), "60 Days", 1, 300, 0, 0);

        var aged90 = new Invoice(batchId, customerId, "INV-004", now.AddDays(-100), now.AddDays(-90), null, null, null, null);
        aged90.AddLine(Guid.NewGuid(), "90+ Days", 1, 400, 0, 0);

        currentDue.Should().Be(100);
        aged30.BalanceDue.Should().Be(200);
        aged60.BalanceDue.Should().Be(300);
        aged90.BalanceDue.Should().Be(400);
    }

    [Fact]
    public void InvoiceVoidBlockedAfterCashApplied()
    {
        var batchId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoice = new Invoice(batchId, customerId, "INV-005", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
        invoice.AddLine(Guid.NewGuid(), "Item", 1, 500, 0, 0);

        var receipt = new CashReceipt(Guid.NewGuid(), customerId, "CR-001", 500, DateTimeOffset.UtcNow, "Check", "USD", null);
        receipt.ApplyToInvoice(invoice, 500);

        invoice.Status.Should().Be(InvoiceStatus.Paid);

        var act = () => invoice.Void();
        act.Should().Throw<InvalidOperationException>().WithMessage("*cash applied*");
    }

    [Fact]
    public void CreditLimitCheckResultComputedCorrectly()
    {
        var creditLimit = 10000m;
        var currentBalance = 3000m;
        var proposedAmount = 2000m;
        var available = creditLimit - currentBalance;

        var isApproved = proposedAmount <= available;
        var result = new CreditLimitCheckResult(isApproved, currentBalance, creditLimit, available, null);

        isApproved.Should().BeTrue();
        result.AvailableCredit.Should().Be(7000);
        result.CurrentBalance.Should().Be(3000);
        result.CreditLimit.Should().Be(10000);
    }

    [Fact]
    public void CashReceiptApplyToInvoiceAndUnapplyWorks()
    {
        var batchId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoice = new Invoice(batchId, customerId, "INV-006", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
        invoice.AddLine(Guid.NewGuid(), "Item A", 2, 250, 0, 0);

        var receipt = new CashReceipt(Guid.NewGuid(), customerId, "CR-002", 500, DateTimeOffset.UtcNow, "Cash", "USD", null);

        receipt.Status.Should().Be(CashReceiptStatus.Unapplied);
        receipt.UnappliedAmount.Should().Be(500);

        var application = receipt.ApplyToInvoice(invoice, 300);

        receipt.Status.Should().Be(CashReceiptStatus.PartiallyApplied);
        receipt.AppliedAmount.Should().Be(300);
        receipt.UnappliedAmount.Should().Be(200);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);

        receipt.UnapplyInvoice(invoice, application);

        receipt.Status.Should().Be(CashReceiptStatus.Unapplied);
        receipt.AppliedAmount.Should().Be(0);
        invoice.Status.Should().Be(InvoiceStatus.Open);
    }

    [Fact]
    public void FinanceChargeCreatedWithCorrectValues()
    {
        var charge = new FinanceCharge(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "FC-20250101-0001",
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            45.83m,
            18.0m,
            "Finance charge on overdue balance");

        charge.ChargeAmount.Should().Be(45.83m);
        charge.AnnualRate.Should().Be(18.0m);
        charge.Status.Should().Be(FinanceChargeStatus.Open);

        charge.Void();
        charge.Status.Should().Be(FinanceChargeStatus.Voided);
    }

    [Fact]
    public void InvoiceStatusTransitionsCorrectly()
    {
        var batchId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoice = new Invoice(batchId, customerId, "INV-007", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
        invoice.AddLine(Guid.NewGuid(), "Service", 1, 1000, 0, 0);

        invoice.Status.Should().Be(InvoiceStatus.Open);

        var receipt = new CashReceipt(Guid.NewGuid(), customerId, "CR-003", 600, DateTimeOffset.UtcNow, "Check", "USD", null);
        receipt.ApplyToInvoice(invoice, 600);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);

        var receipt2 = new CashReceipt(Guid.NewGuid(), customerId, "CR-004", 400, DateTimeOffset.UtcNow, "Check", "USD", null);
        receipt2.ApplyToInvoice(invoice, 400);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void CreditDebitMemoTypeHandlingWorks()
    {
        var batchId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var debitMemo = new CreditDebitMemo(batchId, customerId, "DM-001", DateTimeOffset.UtcNow, null, "Debit memo");
        debitMemo.SetMemoType(CreditDebitMemoType.DebitMemo);
        debitMemo.AddLine(Guid.NewGuid(), "Charge", 1, 500, 0, 0);

        debitMemo.Debit.Should().Be(500);
        debitMemo.Credit.Should().Be(0);
        debitMemo.Status.Should().Be(CreditDebitMemoStatus.Open);

        var creditMemo = new CreditDebitMemo(batchId, customerId, "CM-001", DateTimeOffset.UtcNow, null, "Credit memo");
        creditMemo.SetMemoType(CreditDebitMemoType.CreditMemo);
        creditMemo.AddLine(Guid.NewGuid(), "Refund", 1, 200, 0, 0);

        creditMemo.Credit.Should().Be(200);
        creditMemo.Debit.Should().Be(0);

        creditMemo.Apply();
        creditMemo.Status.Should().Be(CreditDebitMemoStatus.Applied);
    }

    [Fact]
    public void InvoiceBatchStatusLifecycleWorks()
    {
        var batch = new InvoiceBatch(Guid.NewGuid(), "BATCH-001", "Test batch", DateTimeOffset.UtcNow, Guid.NewGuid());

        batch.Status.Should().Be(InvoiceBatchStatus.Draft);

        var invoice = batch.AddInvoice(Guid.NewGuid(), "INV-010", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null);
        invoice.AddLine(Guid.NewGuid(), "Debit", 1, 100, 0, 0);
        invoice.AddLine(Guid.NewGuid(), "Debit", 1, 100, 0, 0);
        var memo = batch.AddCreditDebitMemo(Guid.NewGuid(), "CM-010", DateTimeOffset.UtcNow, null, "Balance");
        memo.SetMemoType(CreditDebitMemoType.CreditMemo);
        memo.AddLine(Guid.NewGuid(), "Credit", 1, 200, 0, 0);

        batch.Release();
        batch.Status.Should().Be(InvoiceBatchStatus.Batched);

        batch.Post();
        batch.Status.Should().Be(InvoiceBatchStatus.Posted);

        var reversal = batch.Reverse("Test reversal");
        batch.Status.Should().Be(InvoiceBatchStatus.Reversed);
        reversal.Status.Should().Be(InvoiceBatchStatus.Draft);
    }
}
