// <copyright file="RequisitionToPOService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Purchasing.Infrastructure;

public class RequisitionToPOService : IRequisitionToPOService
{
    /// <summary>
    /// Requisitions whose total exceeds this amount require approval by a manager
    /// (i.e. a different user than the requestor) before they can be converted to a
    /// PO. Self-approval of a high-value requisition is rejected. This enforces the
    /// separation-of-duties approval threshold documented in the project backlog
    /// (requisition over $10,000 requires manager sign-off).
    /// </summary>
    public const decimal ManagerApprovalThreshold = 10000m;

    private readonly PurchasingDbContext _context;
    private readonly IRepository<Requisition> _requisitionRepository;
    private readonly IRepository<PurchaseOrder> _poRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RequisitionToPOService(
        PurchasingDbContext context,
        IRepository<Requisition> requisitionRepository,
        IRepository<PurchaseOrder> poRepository,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _requisitionRepository = requisitionRepository;
        _poRepository = poRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> ConvertRequisitionToPOAsync(
        Guid requisitionId,
        Guid? preferredVendorId,
        CancellationToken cancellationToken = default)
    {
        var requisition = await _context.Requisitions
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == requisitionId, cancellationToken);

        if (requisition == null)
            throw new InvalidOperationException($"Requisition {requisitionId} not found.");

        if (requisition.Status != RequisitionStatus.Approved)
            throw new InvalidOperationException($"Requisition must be approved before converting to PO. Current status: {requisition.Status}");

        // Separation-of-duties approval threshold: a high-value requisition must be
        // approved by someone other than the requestor (a manager). Self-approval is
        // not sufficient above the threshold.
        if (requisition.GetTotalAmount() > ManagerApprovalThreshold &&
            requisition.ApprovedById == requisition.RequestorId)
        {
            throw new InvalidOperationException(
                $"Requisitions over {ManagerApprovalThreshold:C0} require manager approval (approved by a user other than the requestor). " +
                $"This requisition was self-approved by the requestor.");
        }

        var vendorId = preferredVendorId ?? requisition.Lines.FirstOrDefault()?.PreferredVendorId;
        if (!vendorId.HasValue)
            throw new InvalidOperationException("Vendor ID is required to create PO.");

        var poNumber = await GeneratePONumberAsync(requisition.CompanyId, cancellationToken);

        var po = new PurchaseOrder(
            poNumber,
            requisition.CompanyId,
            vendorId.Value,
            DateTime.UtcNow,
            PurchaseOrderType.Standard,
            null,
            null,
            null,
            null,
            $"Created from Requisition {requisition.RequisitionNumber}",
            null);

        int lineNumber = 1;
        foreach (var reqLine in requisition.Lines.Where(l => !l.IsFullyConverted))
        {
            var poLine = new PurchaseOrderLine(
                po.Id,
                lineNumber++,
                reqLine.ItemId,
                reqLine.Description,
                reqLine.Quantity - reqLine.QuantityConverted,
                reqLine.UnitOfMeasure,
                reqLine.EstimatedUnitPrice,
                reqLine.NeedByDate,
                reqLine.AccountId,
                reqLine.ProjectId,
                reqLine.TaskId,
                reqLine.Id);

            po.AddLine(poLine);
            reqLine.UpdateQuantityConverted(reqLine.Quantity - reqLine.QuantityConverted);
        }

        requisition.MarkAsConverted();

        await _poRepository.AddAsync(po, cancellationToken);
        _requisitionRepository.Update(requisition);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return po.Id;
    }

    public async Task<List<Guid>> ConsolidateRequisitionsToPOAsync(
        List<Guid> requisitionIds,
        Guid vendorId,
        CancellationToken cancellationToken = default)
    {
        var requisitions = await _context.Requisitions
            .Include(r => r.Lines)
            .Where(r => requisitionIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        if (requisitions.Count == 0)
            throw new InvalidOperationException("No requisitions found.");

        var unapproved = requisitions.Where(r => r.Status != RequisitionStatus.Approved).ToList();
        if (unapproved.Count > 0)
            throw new InvalidOperationException($"All requisitions must be approved. Unapproved: {string.Join(", ", unapproved.Select(r => r.RequisitionNumber))}");

        var companyId = requisitions.First().CompanyId;
        if (requisitions.Any(r => r.CompanyId != companyId))
            throw new InvalidOperationException("All requisitions must be from the same company.");

        var poNumber = await GeneratePONumberAsync(companyId, cancellationToken);

        var po = new PurchaseOrder(
            poNumber,
            companyId,
            vendorId,
            DateTime.UtcNow,
            PurchaseOrderType.Standard,
            null,
            null,
            null,
            null,
            $"Consolidated from {requisitions.Count} requisitions",
            null);

        int lineNumber = 1;
        foreach (var requisition in requisitions)
        {
            foreach (var reqLine in requisition.Lines.Where(l => !l.IsFullyConverted))
            {
                var poLine = new PurchaseOrderLine(
                    po.Id,
                    lineNumber++,
                    reqLine.ItemId,
                    reqLine.Description,
                    reqLine.Quantity - reqLine.QuantityConverted,
                    reqLine.UnitOfMeasure,
                    reqLine.EstimatedUnitPrice,
                    reqLine.NeedByDate,
                    reqLine.AccountId,
                    reqLine.ProjectId,
                    reqLine.TaskId,
                    reqLine.Id);

                po.AddLine(poLine);
                reqLine.UpdateQuantityConverted(reqLine.Quantity - reqLine.QuantityConverted);
            }

            requisition.MarkAsConverted();
            _requisitionRepository.Update(requisition);
        }

        await _poRepository.AddAsync(po, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return [po.Id];
    }

    private async Task<string> GeneratePONumberAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var lastPO = await _context.PurchaseOrders
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.OrderDate)
            .FirstOrDefaultAsync(cancellationToken);

        var year = DateTime.UtcNow.Year;
        var sequence = 1;

        if (lastPO != null && lastPO.PONumber.StartsWith($"PO-{year}-", StringComparison.Ordinal))
        {
            var parts = lastPO.PONumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastSequence))
            {
                sequence = lastSequence + 1;
            }
        }

        return $"PO-{year}-{sequence:D6}";
    }
}
