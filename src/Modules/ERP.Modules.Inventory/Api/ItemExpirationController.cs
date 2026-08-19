// <copyright file="ItemExpirationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960
[ApiController]
[Route("api/v1/inventory/expirations")]
public class ItemExpirationController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ItemExpirationController(
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemExpirationDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] ItemExpirationStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] bool includeAlerts = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemExpirations.AsQueryable();

        if (includeAlerts)
        {
            query = query.Include(e => e.Alerts);
        }

        if (companyId.HasValue)
        {
            query = query.Where(e => e.CompanyId == companyId.Value);
        }

        if (itemId.HasValue)
        {
            query = query.Where(e => e.ItemId == itemId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(e => e.WarehouseId == warehouseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.ExpirationDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.ExpirationDate <= endDate.Value);
        }

        var expirations = await query
            .OrderBy(e => e.ExpirationDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = expirations.Select(e => MapToDto(e, includeAlerts)).ToList();
        return Ok(ApiResponse<List<ItemExpirationDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemExpirationDto>>> GetById(
        Guid id,
        [FromQuery] bool includeAlerts = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ItemExpirations.AsQueryable();

        if (includeAlerts)
        {
            query = query.Include(e => e.Alerts);
        }

        var expiration = await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (expiration == null)
        {
            return NotFound(ApiResponse<ItemExpirationDto>.Failure(["Item expiration not found."]));
        }

        return Ok(ApiResponse<ItemExpirationDto>.Success(MapToDto(expiration, includeAlerts)));
    }

    [HttpPost]
    [Authorize(Roles = "InventoryManager,Admin,QualityManager")]
    public async Task<ActionResult<ApiResponse<ItemExpirationDto>>> Create(
        [FromBody] CreateItemExpirationRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _context.Items.FindAsync(new object[] { request.ItemId }, cancellationToken);
        if (item == null)
        {
            return BadRequest(ApiResponse<ItemExpirationDto>.Failure([$"Item {request.ItemId} not found"]));
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { request.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            return BadRequest(ApiResponse<ItemExpirationDto>.Failure([$"Warehouse {request.WarehouseId} not found"]));
        }

        if (request.LotId.HasValue)
        {
            var lot = await _context.Lots.FindAsync(new object[] { request.LotId.Value }, cancellationToken);
            if (lot == null)
            {
                return BadRequest(ApiResponse<ItemExpirationDto>.Failure([$"Lot {request.LotId.Value} not found"]));
            }
        }

        if (request.SerialNumberId.HasValue)
        {
            var serial = await _context.SerialNumbers.FindAsync(new object[] { request.SerialNumberId.Value }, cancellationToken);
            if (serial == null)
            {
                return BadRequest(ApiResponse<ItemExpirationDto>.Failure([$"Serial number {request.SerialNumberId.Value} not found"]));
            }
        }

        var expiration = new ItemExpiration(
            request.CompanyId,
            request.ItemId,
            request.WarehouseId,
            request.LotId,
            request.SerialNumberId,
            request.ExpirationDate,
            request.Quantity,
            request.Notes);

        _context.ItemExpirations.Add(expiration);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(expiration, false);
        return CreatedAtAction(nameof(GetById), new { id = expiration.Id }, ApiResponse<ItemExpirationDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "InventoryManager,Admin,QualityManager")]
    public async Task<ActionResult<ApiResponse<ItemExpirationDto>>> Update(
        Guid id,
        [FromBody] UpdateItemExpirationRequest request,
        CancellationToken cancellationToken)
    {
        var expiration = await _context.ItemExpirations.FindAsync(new object[] { id }, cancellationToken);

        if (expiration == null)
        {
            return NotFound(ApiResponse<ItemExpirationDto>.Failure(["Item expiration not found."]));
        }

        if (request.Quantity.HasValue)
        {
            expiration.UpdateQuantity(request.Quantity.Value);
        }

        if (!string.IsNullOrEmpty(request.Notes))
        {
            expiration.UpdateNotes(request.Notes);
        }

        if (request.Status.HasValue)
        {
            expiration.UpdateStatus(request.Status.Value);
        }

        _context.ItemExpirations.Update(expiration);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemExpirationDto>.Success(MapToDto(expiration, false)));
    }

    [HttpPost("{id:guid}/acknowledge-alert/{alertId:guid}")]
    [Authorize(Roles = "InventoryManager,Admin,QualityManager")]
    public async Task<ActionResult<ApiResponse<ItemExpirationDto>>> AcknowledgeAlert(
        Guid id,
        Guid alertId,
        [FromBody] AcknowledgeAlertRequest request,
        CancellationToken cancellationToken)
    {
        var expiration = await _context.ItemExpirations
            .Include(e => e.Alerts)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (expiration == null)
        {
            return NotFound(ApiResponse<ItemExpirationDto>.Failure(["Item expiration not found."]));
        }

        var alert = expiration.Alerts.FirstOrDefault(a => a.Id == alertId);
        if (alert == null)
        {
            return NotFound(ApiResponse<ItemExpirationDto>.Failure(["Alert not found."]));
        }

        alert.Acknowledge(request.AcknowledgedBy);
        _context.ItemExpirationAlerts.Update(alert);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ItemExpirationDto>.Success(MapToDto(expiration, true)));
    }

    [HttpGet("expiring-soon")]
    public async Task<ActionResult<ApiResponse<List<ItemExpirationDto>>>> GetExpiringSoon(
        [FromQuery] Guid companyId,
        [FromQuery] int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        var expirations = await _context.ItemExpirations
            .Where(e => e.CompanyId == companyId
                     && e.Status == ItemExpirationStatus.Active
                     && e.ExpirationDate <= cutoffDate
                     && e.ExpirationDate >= DateTime.UtcNow)
            .OrderBy(e => e.ExpirationDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = expirations.Select(e => MapToDto(e, false)).ToList();
        return Ok(ApiResponse<List<ItemExpirationDto>>.Success(dtos));
    }

    [HttpGet("expired")]
    public async Task<ActionResult<ApiResponse<List<ItemExpirationDto>>>> GetExpired(
        [FromQuery] Guid companyId,
        [FromQuery] DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var cutoff = asOfDate ?? DateTime.UtcNow;

        var expirations = await _context.ItemExpirations
            .Where(e => e.CompanyId == companyId
                     && e.Status == ItemExpirationStatus.Active
                     && e.ExpirationDate < cutoff)
            .OrderBy(e => e.ExpirationDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var dtos = expirations.Select(e => MapToDto(e, false)).ToList();
        return Ok(ApiResponse<List<ItemExpirationDto>>.Success(dtos));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<ExpirationDashboardDto>>> GetDashboard(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var weekAhead = now.AddDays(7);
        var monthAhead = now.AddDays(30);

        var expiringThisWeek = await _context.ItemExpirations
            .Where(e => e.CompanyId == companyId
                     && e.Status == ItemExpirationStatus.Active
                     && e.ExpirationDate <= weekAhead
                     && e.ExpirationDate >= now)
            .CountAsync(cancellationToken);

        var expiringThisMonth = await _context.ItemExpirations
            .Where(e => e.CompanyId == companyId
                     && e.Status == ItemExpirationStatus.Active
                     && e.ExpirationDate <= monthAhead
                     && e.ExpirationDate >= now)
            .CountAsync(cancellationToken);

        var alreadyExpired = await _context.ItemExpirations
            .Where(e => e.CompanyId == companyId
                     && e.Status == ItemExpirationStatus.Active
                     && e.ExpirationDate < now)
            .CountAsync(cancellationToken);

        var unacknowledgedAlerts = await _context.ItemExpirationAlerts
            .Join(_context.ItemExpirations,
                alert => alert.ItemExpirationId,
                expiration => expiration.Id,
                (alert, expiration) => new { alert, expiration })
            .Where(x => x.expiration.CompanyId == companyId && !x.alert.IsAcknowledged)
            .CountAsync(cancellationToken);

        return Ok(ApiResponse<ExpirationDashboardDto>.Success(new ExpirationDashboardDto
        {
            CompanyId = companyId,
            ExpiringThisWeek = expiringThisWeek,
            ExpiringThisMonth = expiringThisMonth,
            AlreadyExpired = alreadyExpired,
            UnacknowledgedAlerts = unacknowledgedAlerts,
            GeneratedAt = DateTime.UtcNow,
        }));
    }

    [HttpPost("auto-create-from-lots")]
    [Authorize(Roles = "InventoryManager,Admin,QualityManager")]
    public async Task<ActionResult<ApiResponse<int>>> AutoCreateFromLots(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        // Find lots with expiration dates that don't have expiration records
        var lotsWithExpiration = await _context.Lots
            .Where(l => l.ExpirationDate.HasValue
                     && l.Status == LotStatus.Active)
            .ToListAsync(cancellationToken);

        int created = 0;

        foreach (var lot in lotsWithExpiration)
        {
            // Check if expiration record already exists for this lot
            var exists = await _context.ItemExpirations
                .AnyAsync(e => e.LotId == lot.Id, cancellationToken);

            if (!exists)
            {
                // Get stock quantity for this lot
                var availableQty = await _context.InventoryTransactions
                    .Where(t => t.ItemId == lot.ItemId
                             && t.WarehouseId == lot.WarehouseId
                             && t.LotId == lot.Id)
                    .ToListAsync(cancellationToken);

                decimal qty = availableQty
                    .Where(t => t.Quantity > 0).Sum(t => t.Quantity)
                    - availableQty.Where(t => t.Quantity < 0).Sum(t => Math.Abs(t.Quantity));

                if (qty > 0)
                {
                    var expiration = new ItemExpiration(
                        companyId,
                        lot.ItemId,
                        lot.WarehouseId,
                        lot.Id,
                        null,
                        lot.ExpirationDate!.Value,
                        qty,
                        $"Auto-created from lot {lot.LotNumber}");

                    _context.ItemExpirations.Add(expiration);
                    created++;
                }
            }
        }

        if (created > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Ok(ApiResponse<int>.Success(created));
    }

    [HttpPost("generate-alerts")]
    [Authorize(Roles = "InventoryManager,Admin,QualityManager")]
    public async Task<ActionResult<ApiResponse<int>>> GenerateAlerts(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var criticalDate = now.AddDays(7);
        var warningDate = now.AddDays(30);

        var expirations = await _context.ItemExpirations
            .Where(e => e.CompanyId == companyId
                     && e.Status == ItemExpirationStatus.Active
                     && e.ExpirationDate <= warningDate
                     && e.ExpirationDate >= now)
            .ToListAsync(cancellationToken);

        int alertsCreated = 0;

        foreach (var expiration in expirations)
        {
            // Check if alert already exists for this period
            var existingAlert = await _context.ItemExpirationAlerts
                .FirstOrDefaultAsync(a => a.ItemExpirationId == expiration.Id
                                     && a.AlertDate == now.Date, cancellationToken);

            if (existingAlert != null)
                continue;

            ExpirationAlertType alertType;
            string message;

            if (expiration.ExpirationDate <= now)
            {
                alertType = ExpirationAlertType.Expired;
                message = $"Item has expired on {expiration.ExpirationDate:yyyy-MM-dd}";
            }
            else if (expiration.ExpirationDate <= criticalDate)
            {
                alertType = ExpirationAlertType.Critical;
                message = $"Item expires in {(expiration.ExpirationDate - now).Days} days on {expiration.ExpirationDate:yyyy-MM-dd}";
            }
            else
            {
                alertType = ExpirationAlertType.Warning;
                message = $"Item expires in {(expiration.ExpirationDate - now).Days} days on {expiration.ExpirationDate:yyyy-MM-dd}";
            }

            var alert = new ItemExpirationAlert(
                expiration.Id,
                alertType,
                now.Date,
                message);

            expiration.AddAlert(alert);
            expiration.UpdateStatus(ItemExpirationStatus.Alerted);
            alertsCreated++;
        }

        if (alertsCreated > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Ok(ApiResponse<int>.Success(alertsCreated));
    }

    private static ItemExpirationDto MapToDto(ItemExpiration expiration, bool includeAlerts = false)
    {
        var dto = new ItemExpirationDto
        {
            Id = expiration.Id,
            CompanyId = expiration.CompanyId,
            ItemId = expiration.ItemId,
            WarehouseId = expiration.WarehouseId,
            LotId = expiration.LotId,
            SerialNumberId = expiration.SerialNumberId,
            ExpirationDate = expiration.ExpirationDate,
            Quantity = expiration.Quantity,
            Notes = expiration.Notes,
            Status = expiration.Status.ToString(),
            CreatedAt = expiration.CreatedOn,
            CreatedBy = expiration.CreatedBy,
        };

        if (includeAlerts)
        {
            dto.Alerts.AddRange(expiration.Alerts.Select(a => new ItemExpirationAlertDto
            {
                Id = a.Id,
                ItemExpirationId = a.ItemExpirationId,
                AlertType = a.AlertType.ToString(),
                AlertDate = a.AlertDate,
                Message = a.Message,
                IsAcknowledged = a.IsAcknowledged,
                AcknowledgedBy = a.AcknowledgedBy,
                AcknowledgedDate = a.AcknowledgedDate,
            }));
        }

        return dto;
    }
}

#pragma warning disable CA1002, CA2227
public class ItemExpirationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? SerialNumberId { get; set; }
    public DateTime ExpirationDate { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<ItemExpirationAlertDto> Alerts { get; } = new List<ItemExpirationAlertDto>();
}
#pragma warning restore CA1002, CA2227

public class ItemExpirationAlertDto
{
    public Guid Id { get; set; }
    public Guid ItemExpirationId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public DateTime AlertDate { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsAcknowledged { get; set; }
    public Guid? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedDate { get; set; }
}

public class CreateItemExpirationRequest
{
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? SerialNumberId { get; set; }
    public DateTime ExpirationDate { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

public class UpdateItemExpirationRequest
{
    public decimal? Quantity { get; set; }
    public string? Notes { get; set; }
    public ItemExpirationStatus? Status { get; set; }
}

public class AcknowledgeAlertRequest
{
    public Guid AcknowledgedBy { get; set; }
}

#pragma warning disable CA1002, CA2227
public class ExpirationDashboardDto
{
    public Guid CompanyId { get; set; }
    public int ExpiringThisWeek { get; set; }
    public int ExpiringThisMonth { get; set; }
    public int AlreadyExpired { get; set; }
    public int UnacknowledgedAlerts { get; set; }
    public DateTime GeneratedAt { get; set; }
}
#pragma warning restore CA1002, CA2227