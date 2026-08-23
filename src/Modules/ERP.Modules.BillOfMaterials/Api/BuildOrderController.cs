// <copyright file="BuildOrderController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.BillOfMaterials.Domain.Entities;
using ERP.Modules.BillOfMaterials.Infrastructure;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Domain.Events;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InvInventoryContext = ERP.Modules.Inventory.Infrastructure.InventoryDbContext;
using InvIUnitOfWork = ERP.Modules.Inventory.IUnitOfWork;

namespace ERP.Modules.BillOfMaterials.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
/// <summary>
/// Build order lifecycle endpoints (create / release / complete / disassemble / backflush).
/// </summary>
[ApiController]
[Route("api/v1/bom/build-orders")]
public partial class BuildOrderController : ControllerBase
{
    private readonly BomDbContext _bomContext;
    private readonly InvInventoryContext _invContext;
    private readonly IBomUnitOfWork _bomUnitOfWork;
    private readonly InvIUnitOfWork _invUnitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IComponentReservationService _reservationService;
    private readonly ILogger<BuildOrderController> _logger;

    public BuildOrderController(
        BomDbContext bomContext,
        InvInventoryContext invContext,
        IBomUnitOfWork bomUnitOfWork,
        InvIUnitOfWork invUnitOfWork,
        IDomainEventDispatcher eventDispatcher,
        IComponentReservationService reservationService,
        ILogger<BuildOrderController> logger)
    {
        _bomContext = bomContext;
        _invContext = invContext;
        _bomUnitOfWork = bomUnitOfWork;
        _invUnitOfWork = invUnitOfWork;
        _eventDispatcher = eventDispatcher;
        _reservationService = reservationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BuildOrderDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] BuildOrderStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _bomContext.BuildOrders
            .Include(b => b.Lines)
            .AsQueryable();

        query = query.ApplyCompanyScope(HttpContext, b => b.CompanyId, companyId);

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        var orders = await query.OrderByDescending(b => b.BuildDate).ToListAsync(cancellationToken);
        var dtos = orders.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<BuildOrderDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BuildOrderDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _bomContext.BuildOrders
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (order is null)
        {
            return NotFound(ApiResponse<BuildOrderDto>.Failure(new[] { "Build order not found." }, 404));
        }

        return Ok(ApiResponse<BuildOrderDto>.Success(MapToDto(order)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateBuildOrderRequest request,
        CancellationToken cancellationToken)
    {
        // Validate BOM exists and is active
        var bom = await _bomContext.BomHeaders
            .Include(h => h.Components)
            .FirstOrDefaultAsync(h => h.Id == request.BomHeaderId, cancellationToken);

        if (bom is null)
        {
            return BadRequest(ApiResponse.Failure(new[] { "BOM not found." }));
        }

        if (bom.Status != BomStatus.Active)
        {
            return BadRequest(ApiResponse.Failure(new[] { "BOM must be active to create a build order." }));
        }

        var txnType = Enum.TryParse<BuildTransactionType>(request.TransactionType, true, out var parsed) ? parsed : BuildTransactionType.Assemble;

        var order = new BuildOrder(
            request.CompanyId,
            request.BuildNumber,
            txnType,
            request.BomHeaderId,
            request.ParentItemId,
            request.QuantityToBuild,
            request.UnitOfMeasure,
            request.WarehouseId,
            request.BuildDate,
            request.Notes);

        // Auto-populate component lines from BOM
        foreach (var comp in bom.Components)
        {
            var qtyRequired = comp.EffectiveQuantity * request.QuantityToBuild;
            if (bom.YieldPercentage > 0 && bom.YieldPercentage != 100)
            {
                qtyRequired = qtyRequired / (bom.YieldPercentage / 100m);
            }

            // Look up component item cost
            var itemCost = await _invContext.Items
                .Where(i => i.Id == comp.ComponentItemId)
                .Select(i => i.StandardCost)
                .FirstOrDefaultAsync(cancellationToken);

            order.AddLine(
                comp.ComponentItemId,
                qtyRequired,
                0,
                comp.UnitOfMeasure,
                itemCost ?? 0,
                notes: comp.Notes);
        }

        _bomContext.BuildOrders.Add(order);
        await _bomUnitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(order.Id));
    }

    [HttpPost("{id:guid}/release")]
    public async Task<ActionResult<ApiResponse>> Release(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _bomContext.BuildOrders
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (order is null)
        {
            return NotFound(ApiResponse.Failure(new[] { "Build order not found." }, 404));
        }

        if (order.Status != BuildOrderStatus.Draft && order.Status != BuildOrderStatus.Planned)
        {
            return BadRequest(ApiResponse.Failure(new[] { "Only Draft or Planned orders can be released." }));
        }

        // Check component availability
        var shortages = new List<string>();
        foreach (var line in order.Lines.Where(l => !l.IsLabor && !l.IsOverhead))
        {
            var onHand = await _invContext.ItemStocks
                .Where(s => s.ItemId == line.ComponentItemId && s.WarehouseId == order.WarehouseId)
                .SumAsync(s => s.OnHandQuantity - s.AllocatedQuantity, cancellationToken);

            if (onHand < line.QuantityRequired)
            {
                var itemCode = await _invContext.Items
                    .Where(i => i.Id == line.ComponentItemId)
                    .Select(i => i.ItemCode)
                    .FirstOrDefaultAsync(cancellationToken) ?? line.ComponentItemId.ToString();

                shortages.Add($"{itemCode}: needs {line.QuantityRequired}, available {onHand}");
            }
        }

        if (shortages.Count > 0)
        {
            return BadRequest(ApiResponse.Failure(new[] { $"Component shortages: {string.Join("; ", shortages)}" }));
        }

        order.UpdateStatus(BuildOrderStatus.Released);
        await _bomUnitOfWork.SaveChangesAsync(cancellationToken);

        var components = order.Lines
            .Where(l => !l.IsLabor && !l.IsOverhead && l.QuantityRequired > 0)
            .Select(l => new ComponentReservationRequest(l.ComponentItemId, l.QuantityRequired, l.UnitOfMeasure))
            .ToList();

        int reservedCount = 0;
        try
        {
            reservedCount = await _reservationService.ReserveForBuildOrderAsync(
                order.CompanyId, order.Id, components, cancellationToken);
        }
#pragma warning disable CA1031 // Reservation failure must not block a released build order
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogReservationFailure(ex, order.Id);
        }

        return Ok(ApiResponse.Success(
            reservedCount > 0
                ? $"Build order released. {reservedCount} component reservations created."
                : "Build order released."));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Component reservation failed for build order {BuildOrderId}.")]
    private partial void LogReservationFailure(Exception exception, Guid buildOrderId);

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse>> Complete(
        Guid id,
        [FromBody] CompleteBuildRequest? request,
        CancellationToken cancellationToken)
    {
        var order = await _bomContext.BuildOrders
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (order is null)
        {
            return NotFound(ApiResponse.Failure(new[] { "Build order not found." }, 404));
        }

        if (order.Status != BuildOrderStatus.Released && order.Status != BuildOrderStatus.InProgress)
        {
            return BadRequest(ApiResponse.Failure(new[] { "Only Released or In-Progress orders can be completed." }));
        }

        // Issue components from inventory
        foreach (var line in order.Lines.Where(l => !l.IsLabor && !l.IsOverhead))
        {
            var qtyIssued = request?.ActualQuantities?.GetValueOrDefault(line.ComponentItemId) ?? line.QuantityRequired;
            line.UpdateIssuedQuantity(qtyIssued, line.UnitCost);
            line.CalculateVariance();

            // Create inventory issue transaction (negative qty = issue)
            var issueTxn = new InventoryTransaction(
                order.CompanyId,
                line.ComponentItemId,
                order.WarehouseId,
                TransactionType.Issue,
                -qtyIssued,
                line.UnitOfMeasure,
                line.UnitCost,
                order.BuildDate,
                referenceNumber: order.BuildNumber,
                notes: $"Build order {order.BuildNumber} component issue");

            _invContext.InventoryTransactions.Add(issueTxn);
        }

        // Calculate total costs
        order.CalculateCosts();
        order.UpdateStatus(BuildOrderStatus.InProgress);

        await _invUnitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch events for GL posting (component issues)
        foreach (var txn in _invContext.ChangeTracker.Entries<InventoryTransaction>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity))
        {
            await _eventDispatcher.DispatchAsync(
                new InventoryTransactionPostedEvent(
                    txn.Id,
                    txn.CompanyId,
                    txn.ItemId,
                    txn.WarehouseId,
                    txn.TransactionType.ToString(),
                    txn.Quantity,
                    txn.UnitCost,
                    txn.ExtendedCost,
                    txn.TransactionDate,
                    null),
                cancellationToken);
        }

        // Create inventory receipt for parent item (the assembled product)
        var parentItem = await _invContext.Items
            .FirstOrDefaultAsync(i => i.Id == order.ParentItemId, cancellationToken);

        var unitCost = order.UnitCost ?? parentItem?.StandardCost ?? 0;
        var qtyBuilt = request?.ActualYield ?? order.QuantityToBuild;

        var receiptTxn = new InventoryTransaction(
            order.CompanyId,
            order.ParentItemId,
            order.WarehouseId,
            TransactionType.Receipt,
            qtyBuilt,
            order.UnitOfMeasure,
            unitCost,
            order.BuildDate,
            referenceNumber: order.BuildNumber,
            notes: $"Build order {order.BuildNumber} assembly receipt");

        _invContext.InventoryTransactions.Add(receiptTxn);
        await _invUnitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch event for GL posting (parent receipt)
        await _eventDispatcher.DispatchAsync(
            new InventoryTransactionPostedEvent(
                receiptTxn.Id,
                receiptTxn.CompanyId,
                receiptTxn.ItemId,
                receiptTxn.WarehouseId,
                receiptTxn.TransactionType.ToString(),
                receiptTxn.Quantity,
                receiptTxn.UnitCost,
                receiptTxn.ExtendedCost,
                receiptTxn.TransactionDate,
                null),
            cancellationToken);

        // Mark build complete
        order.UpdateStatus(BuildOrderStatus.Completed);
        order.SetPosted(Guid.Empty);
        await _bomUnitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Success($"Build completed. {qtyBuilt} units produced at ${unitCost:F4}/unit."));
    }

    [HttpPost("{id:guid}/disassemble")]
    public async Task<ActionResult<ApiResponse>> Disassemble(
        Guid id,
        [FromBody] DisassembleRequest? request,
        CancellationToken cancellationToken)
    {
        var order = await _bomContext.BuildOrders
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (order is null)
        {
            return NotFound(ApiResponse.Failure(new[] { "Build order not found." }, 404));
        }

        // Create reverse build order
        var reverseOrder = new BuildOrder(
            order.CompanyId,
            $"{order.BuildNumber}-DIS",
            BuildTransactionType.Disassemble,
            order.BomHeaderId,
            order.ParentItemId,
            request?.Quantity ?? order.QuantityToBuild,
            order.UnitOfMeasure,
            order.WarehouseId,
            DateTime.UtcNow,
            $"Disassembly of {order.BuildNumber}");

        // Add reverse lines (components go back to inventory)
        foreach (var line in order.Lines.Where(l => !l.IsLabor && !l.IsOverhead))
        {
            var qtyReturned = (request?.Quantity ?? order.QuantityToBuild) / order.QuantityToBuild * line.QuantityIssued;

            // Return components to inventory
            var receiptTxn = new InventoryTransaction(
                order.CompanyId,
                line.ComponentItemId,
                order.WarehouseId,
                TransactionType.Receipt,
                qtyReturned,
                line.UnitOfMeasure,
                line.UnitCost,
                DateTime.UtcNow,
                referenceNumber: reverseOrder.BuildNumber,
                notes: $"Disassembly of {order.BuildNumber}");

            _invContext.InventoryTransactions.Add(receiptTxn);

            reverseOrder.AddLine(
                line.ComponentItemId,
                line.QuantityIssued,
                qtyReturned,
                line.UnitOfMeasure,
                line.UnitCost,
                notes: $"Returned from disassembly");
        }

        // Consume parent item
        var issueTxn = new InventoryTransaction(
            order.CompanyId,
            order.ParentItemId,
            order.WarehouseId,
            TransactionType.Issue,
            -(request?.Quantity ?? order.QuantityToBuild),
            order.UnitOfMeasure,
            order.UnitCost ?? 0,
            DateTime.UtcNow,
            referenceNumber: reverseOrder.BuildNumber,
            notes: $"Disassembly of {order.BuildNumber} parent consumed");

        _invContext.InventoryTransactions.Add(issueTxn);

        reverseOrder.CalculateCosts();
        reverseOrder.SetPosted(Guid.Empty);
        reverseOrder.UpdateStatus(BuildOrderStatus.Completed);

        _bomContext.BuildOrders.Add(reverseOrder);
        await _bomUnitOfWork.SaveChangesAsync(cancellationToken);
        await _invUnitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch events for all transactions
        foreach (var txn in _invContext.ChangeTracker.Entries<InventoryTransaction>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity))
        {
            await _eventDispatcher.DispatchAsync(
                new InventoryTransactionPostedEvent(
                    txn.Id,
                    txn.CompanyId,
                    txn.ItemId,
                    txn.WarehouseId,
                    txn.TransactionType.ToString(),
                    txn.Quantity,
                    txn.UnitCost,
                    txn.ExtendedCost,
                    txn.TransactionDate,
                    null),
                cancellationToken);
        }

        return Ok(ApiResponse.Success("Disassembly complete. Parent consumed, components restocked."));
    }

    // --- Backflush component consumption (677) ---
    [HttpPost("{id:guid}/backflush")]
    public async Task<ActionResult<ApiResponse<BackflushResultDto>>> Backflush(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _bomContext.BuildOrders
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (order is null)
            return NotFound(ApiResponse.Failure(new[] { "Build order not found." }, 404));

        var bom = await _bomContext.BomHeaders
            .Include(h => h.Components)
            .FirstOrDefaultAsync(h => h.Id == order.BomHeaderId, cancellationToken);

        if (bom is null)
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));

        // Backflush: consume standard component qty for the built quantity.
        decimal standardCost = 0;
        decimal actualCost = 0;
        var issued = new List<Guid>();

        foreach (var comp in bom.Components)
        {
            var standardQty = order.QuantityToBuild * comp.EffectiveQuantity;
            if (standardQty <= 0)
                continue;

            var unitCost = comp.EstimatedUnitCost
                ?? await _invContext.Items.Where(i => i.Id == comp.ComponentItemId)
                    .Select(i => i.StandardCost ?? 0).FirstOrDefaultAsync(cancellationToken);

            // Actual previously-issued quantity for this component on this build (manual issues).
            var previouslyIssued = await _invContext.InventoryTransactions
                .Where(t => t.ItemId == comp.ComponentItemId
                            && t.WarehouseId == order.WarehouseId
                            && t.TransactionType == TransactionType.Issue
                            && t.ReferenceNumber == order.BuildNumber)
                .SumAsync(t => -t.Quantity, cancellationToken);

            var varianceQty = standardQty - previouslyIssued;
            standardCost += standardQty * unitCost;
            actualCost += previouslyIssued * unitCost;

            if (varianceQty > 0)
            {
                var issueTxn = new InventoryTransaction(
                    order.CompanyId,
                    comp.ComponentItemId,
                    order.WarehouseId,
                    TransactionType.Issue,
                    -varianceQty,
                    comp.UnitOfMeasure,
                    unitCost,
                    DateTime.UtcNow,
                    referenceNumber: order.BuildNumber,
                    notes: $"Backflush of {order.BuildNumber}");
                _invContext.InventoryTransactions.Add(issueTxn);
                issued.Add(issueTxn.Id);
            }
        }

        var record = new BackflushRecord(order.CompanyId, order.Id, bom.Id, order.QuantityToBuild);
        record.SetCosts(standardCost, actualCost);
        record.MarkPosted();
        _bomContext.BackflushRecords.Add(record);

        await _bomUnitOfWork.SaveChangesAsync(cancellationToken);
        await _invUnitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch inventory events so GL / project-ledger dual-posting fires.
        foreach (var txn in _invContext.ChangeTracker.Entries<InventoryTransaction>()
            .Where(e => e.State == EntityState.Added).Select(e => e.Entity))
        {
            await _eventDispatcher.DispatchAsync(
                new InventoryTransactionPostedEvent(
                    txn.Id,
                    txn.CompanyId,
                    txn.ItemId,
                    txn.WarehouseId,
                    txn.TransactionType.ToString(),
                    txn.Quantity,
                    txn.UnitCost,
                    txn.ExtendedCost,
                    txn.TransactionDate,
                    null),
                cancellationToken);
        }

        return Ok(ApiResponse<BackflushResultDto>.Success(new BackflushResultDto
        {
            BuildOrderId = order.Id,
            QuantityBuilt = order.QuantityToBuild,
            StandardComponentCost = standardCost,
            ActualComponentCost = actualCost,
            Variance = actualCost - standardCost,
            TransactionsCreated = issued.Count,
        }));
    }

    // --- Mapping ---
    private static BuildOrderDto MapToDto(BuildOrder b) => new ()
    {
        Id = b.Id,
        CompanyId = b.CompanyId,
        BuildNumber = b.BuildNumber,
        TransactionType = b.TransactionType.ToString(),
        BomHeaderId = b.BomHeaderId,
        ParentItemId = b.ParentItemId,
        QuantityToBuild = b.QuantityToBuild,
        UnitOfMeasure = b.UnitOfMeasure,
        WarehouseId = b.WarehouseId,
        BuildDate = b.BuildDate,
        Status = b.Status.ToString(),
        ActualYield = b.ActualYield,
        TotalMaterialCost = b.TotalMaterialCost,
        TotalLaborCost = b.TotalLaborCost,
        TotalOverheadCost = b.TotalOverheadCost,
        TotalCost = b.TotalCost,
        UnitCost = b.UnitCost,
        Notes = b.Notes,
        LineCount = b.Lines.Count,
    };
}

// --- DTOs ---
#pragma warning disable S6960

public class BuildOrderDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string BuildNumber { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public Guid BomHeaderId { get; set; }
    public Guid ParentItemId { get; set; }
    public decimal QuantityToBuild { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public DateTime BuildDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? ActualYield { get; set; }
    public decimal? TotalMaterialCost { get; set; }
    public decimal? TotalLaborCost { get; set; }
    public decimal? TotalOverheadCost { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
    public int LineCount { get; set; }
}

public class CreateBuildOrderRequest
{
    public Guid CompanyId { get; set; }
    public string BuildNumber { get; set; } = string.Empty;
    public string TransactionType { get; set; } = "Assemble";
    public Guid BomHeaderId { get; set; }
    public Guid ParentItemId { get; set; }
    public decimal QuantityToBuild { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public Guid WarehouseId { get; set; }
    public DateTime BuildDate { get; set; }
    public string? Notes { get; set; }
}

#pragma warning disable CA2227
public class CompleteBuildRequest
{
    public decimal? ActualYield { get; set; }
    public Dictionary<Guid, decimal>? ActualQuantities { get; set; }
}
#pragma warning restore CA2227

public class DisassembleRequest
{
    public decimal? Quantity { get; set; }
}

public class BackflushResultDto
{
    public Guid BuildOrderId { get; set; }
    public decimal QuantityBuilt { get; set; }
    public decimal StandardComponentCost { get; set; }
    public decimal ActualComponentCost { get; set; }
    public decimal Variance { get; set; }
    public int TransactionsCreated { get; set; }
}
