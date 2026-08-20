// <copyright file="ShipmentConfirmedToArHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.OrderManagement.Domain.Events;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="ShipmentConfirmedEvent"/> (raised when a sales shipment is
/// confirmed) and generates the customer invoice. The invoice batch is released and
/// posted, which raises <c>InvoiceBatchPostedEvent</c> and flows to the General
/// Ledger through the canonical AR -&gt; GL posting handler. This closes the Sales
/// -&gt; AR -&gt; GL leg of the integrated Purchase -&gt; Inventory -&gt; Sales flow.
/// </summary>
public sealed class ShipmentConfirmedToArHandler : IDomainEventHandler<ShipmentConfirmedEvent>
{
    private const string RevenueAccountNumber = "4000";

    private readonly ArDbContext _arContext;
    private readonly PlatformDbContext _platformContext;
    private readonly IPeriodService _periodService;

    public ShipmentConfirmedToArHandler(
        ArDbContext arContext,
        PlatformDbContext platformContext,
        IPeriodService periodService)
    {
        _arContext = arContext ?? throw new ArgumentNullException(nameof(arContext));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
        _periodService = periodService ?? throw new ArgumentNullException(nameof(periodService));
    }

    public async Task HandleAsync(ShipmentConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Lines.Count == 0)
            return;

        var customer = await _arContext.Customers
            .FirstOrDefaultAsync(c => c.Id == domainEvent.CustomerId, cancellationToken);
        if (customer is null)
            return; // Customer not yet migrated into AR; nothing to invoice.

        // Resolve the default revenue account so GL posting never references Guid.Empty.
        var revenueAccountId = await ResolveRevenueAccountIdAsync(domainEvent.CompanyId, cancellationToken);

        var period = await _periodService.GetCurrentPeriodAsync(domainEvent.CompanyId, cancellationToken);
        var postingDate = DateTimeOffset.UtcNow;
        var invoiceDate = DateTimeOffset.UtcNow;
        var dueDate = invoiceDate.AddDays(customer.CreditHoldDays > 0 ? customer.CreditHoldDays : 30);

        var batch = new InvoiceBatch(
            domainEvent.CompanyId,
            $"SHIP-{domainEvent.ShipmentNumber}",
            $"Invoice from shipment {domainEvent.ShipmentNumber}",
            postingDate,
            period?.Id ?? Guid.Empty);

        foreach (var line in domainEvent.Lines)
        {
            var invoice = batch.AddInvoice(
                domainEvent.CustomerId,
                $"INV-{domainEvent.ShipmentNumber}-{line.LineNumber}",
                invoiceDate,
                dueDate,
                line.Description,
                customer.DefaultPaymentTermId,
                line.ProjectId,
                domainEvent.SalesOrderId);

            var discountAmount = (line.Quantity * line.UnitPrice) * (line.DiscountPercent / 100m);
            var taxAmount = ((line.Quantity * line.UnitPrice) - discountAmount) * (line.TaxPercent / 100m);
            var accountId = line.AccountId ?? revenueAccountId;

            invoice.AddLine(
                accountId,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                taxAmount,
                discountAmount);
        }

        if (domainEvent.FreightCost > 0)
        {
            var freightInvoice = batch.AddInvoice(
                domainEvent.CustomerId,
                $"FRT-{domainEvent.ShipmentNumber}",
                invoiceDate,
                dueDate,
                $"Freight for shipment {domainEvent.ShipmentNumber}",
                customer.DefaultPaymentTermId,
                null,
                domainEvent.SalesOrderId);
            freightInvoice.AddLine(revenueAccountId, "Freight", 1, domainEvent.FreightCost, 0, 0);
        }

        _arContext.InvoiceBatches.Add(batch);
        batch.Release();
        batch.Post();
        await _arContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the company's revenue account (4000) so invoice lines always reference a
    /// valid GL account during AR -&gt; GL posting. Falls back to Guid.Empty only if the
    /// chart of accounts has not been seeded for the company (platform configuration gap).
    /// </summary>
    private async Task<Guid> ResolveRevenueAccountIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var account = await _platformContext.Accounts
            .Where(a => a.CompanyId == companyId && a.AccountNumber == RevenueAccountNumber && !a.DeletedOn.HasValue)
            .OrderBy(a => a.AccountNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return account?.Id ?? Guid.Empty;
    }
}
