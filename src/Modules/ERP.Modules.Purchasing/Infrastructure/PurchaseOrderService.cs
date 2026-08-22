// <copyright file="PurchaseOrderService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Modules.Purchasing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Purchasing.Infrastructure;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly PurchasingDbContext _context;
    private readonly IRepository<PurchaseOrder> _poRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBudgetAvailabilityCheck _budgetAvailabilityCheck;

    public PurchaseOrderService(
        PurchasingDbContext context,
        IRepository<PurchaseOrder> poRepository,
        IUnitOfWork unitOfWork,
        IBudgetAvailabilityCheck budgetAvailabilityCheck)
    {
        _context = context;
        _poRepository = poRepository;
        _unitOfWork = unitOfWork;
        _budgetAvailabilityCheck = budgetAvailabilityCheck;
    }

    public async Task<Guid> CreateChangeOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, cancellationToken);

        if (po == null)
            throw new InvalidOperationException($"Purchase order {purchaseOrderId} not found.");

        po.CreateChangeOrder();
        _poRepository.Update(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return po.Id;
    }

    public async Task<bool> IsAutoClosureEligibleAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, cancellationToken);

        if (po == null)
            return false;

        if (po.Status != PurchaseOrderStatus.Approved)
            return false;

        if (po.OrderType == PurchaseOrderType.Blanket || po.OrderType == PurchaseOrderType.Standing)
            return false;

        return po.IsFullyReceived() && po.IsFullyInvoiced();
    }

    public async Task AutoClosePurchaseOrdersAsync(int daysOld = 90, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var eligiblePOs = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .Where(p => p.Status == PurchaseOrderStatus.Approved)
            .Where(p => p.OrderDate <= cutoffDate)
            .Where(p => p.OrderType != PurchaseOrderType.Blanket && p.OrderType != PurchaseOrderType.Standing)
            .ToListAsync(cancellationToken);

        var closedCount = 0;
        foreach (var po in eligiblePOs)
        {
            if (po.IsFullyReceived() && po.IsFullyInvoiced())
            {
                po.Close($"Auto-closed: fully received and invoiced, older than {daysOld} days");
                _poRepository.Update(po);
                closedCount++;
            }
        }

        if (closedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<decimal> CalculateCommittedCostAsync(
        Guid? projectId,
        Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrderLines
            .Include(l => l)
            .Where(l => !l.IsCancelled);

        if (projectId.HasValue)
            query = query.Where(l => l.ProjectId == projectId.Value);

        if (accountId.HasValue)
            query = query.Where(l => l.AccountId == accountId.Value);

        var lines = await query.ToListAsync(cancellationToken);

        return lines.Sum(l => l.GetRemainingAmount());
    }

    public async Task<PurchaseOrder> ApproveWithBudgetCheckAsync(
        Guid purchaseOrderId,
        Guid approvedById,
        bool budgetOverride = false,
        CancellationToken cancellationToken = default)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, cancellationToken);

        if (po == null)
            throw new InvalidOperationException($"Purchase order {purchaseOrderId} not found.");

        if (!budgetOverride)
            await EnsureWithinBudgetAsync(po, cancellationToken);

        po.Approve(approvedById);
        _poRepository.Update(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return po;
    }

    private async Task EnsureWithinBudgetAsync(PurchaseOrder po, CancellationToken cancellationToken)
    {
        var chargeGroups = po.Lines
            .Where(l => !l.IsCancelled && (l.ProjectId.HasValue || l.AccountId.HasValue))
            .Select(l => new { l.ProjectId, l.AccountId })
            .Distinct()
            .ToList();

        foreach (var chargeGroup in chargeGroups)
        {
            var committed = await CalculateCommittedCostAsync(chargeGroup.ProjectId, chargeGroup.AccountId, cancellationToken);
            var remaining = await _budgetAvailabilityCheck.GetRemainingBudgetAsync(
                po.CompanyId, chargeGroup.ProjectId, chargeGroup.AccountId, cancellationToken);

            if (committed > remaining)
                throw new InvalidOperationException(
                    $"Budget exceeded for project/account commitment. Committed cost {committed} exceeds remaining budget {remaining}. Override required.");
        }
    }
}
