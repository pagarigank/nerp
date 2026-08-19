// <copyright file="VendorItemController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing/vendor-items")]
public class VendorItemController : ControllerBase
{
    private readonly IRepository<VendorItem> _vendorItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VendorItemController(
        IRepository<VendorItem> vendorItemRepository,
        IUnitOfWork unitOfWork)
    {
        _vendorItemRepository = vendorItemRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<VendorItemDto>>>> GetAll(
        [FromQuery] Guid? vendorId,
        [FromQuery] string? itemId,
        CancellationToken cancellationToken)
    {
        var items = await _vendorItemRepository.GetAllAsync(cancellationToken);

        if (vendorId.HasValue)
            items = items.Where(vi => vi.VendorId == vendorId.Value).ToList();

        if (!string.IsNullOrWhiteSpace(itemId))
            items = items.Where(vi => vi.ItemId == itemId).ToList();

        var dtos = items.Select(vi => new VendorItemDto
        {
            Id = vi.Id,
            VendorId = vi.VendorId,
            ItemId = vi.ItemId,
            VendorItemCode = vi.VendorItemCode,
            VendorDescription = vi.VendorDescription,
            Cost = vi.Cost,
            LeadTimeDays = vi.LeadTimeDays,
            MinimumOrderQuantity = vi.MinimumOrderQuantity,
            IsActive = vi.IsActive,
            IsPrimaryVendor = vi.IsPrimaryVendor,
        }).ToList();

        return Ok(ApiResponse<List<VendorItemDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VendorItemDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _vendorItemRepository.GetByIdAsync(id, cancellationToken);

        if (item == null)
        {
            return NotFound(ApiResponse<VendorItemDto>.Failure(["Vendor item not found."]));
        }

        var dto = new VendorItemDto
        {
            Id = item.Id,
            VendorId = item.VendorId,
            ItemId = item.ItemId,
            VendorItemCode = item.VendorItemCode,
            VendorDescription = item.VendorDescription,
            Cost = item.Cost,
            LeadTimeDays = item.LeadTimeDays,
            MinimumOrderQuantity = item.MinimumOrderQuantity,
            IsActive = item.IsActive,
            IsPrimaryVendor = item.IsPrimaryVendor,
        };

        return Ok(ApiResponse<VendorItemDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VendorItemDto>>> Create(
        [FromBody] CreateVendorItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = new VendorItem(
            request.VendorId,
            request.ItemId,
            request.VendorItemCode,
            request.VendorDescription,
            request.Cost,
            request.LeadTimeDays,
            request.MinimumOrderQuantity,
            request.IsActive,
            request.IsPrimaryVendor);

        await _vendorItemRepository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new VendorItemDto
        {
            Id = item.Id,
            VendorId = item.VendorId,
            ItemId = item.ItemId,
            VendorItemCode = item.VendorItemCode,
            VendorDescription = item.VendorDescription,
            Cost = item.Cost,
            LeadTimeDays = item.LeadTimeDays,
            MinimumOrderQuantity = item.MinimumOrderQuantity,
            IsActive = item.IsActive,
            IsPrimaryVendor = item.IsPrimaryVendor,
        };

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ApiResponse<VendorItemDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VendorItemDto>>> Update(
        Guid id,
        [FromBody] UpdateVendorItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _vendorItemRepository.GetByIdAsync(id, cancellationToken);

        if (item == null)
        {
            return NotFound(ApiResponse<VendorItemDto>.Failure(["Vendor item not found."]));
        }

        item.UpdateCost(request.Cost, request.EffectiveDate);
        item.UpdateLeadTime(request.LeadTimeDays);
        item.UpdateMinimumOrderQuantity(request.MinimumOrderQuantity);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                item.Activate();
            else
                item.Deactivate();
        }

        if (request.IsPrimaryVendor.HasValue)
        {
            item.SetPrimaryVendor(request.IsPrimaryVendor.Value);
        }

        _vendorItemRepository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new VendorItemDto
        {
            Id = item.Id,
            VendorId = item.VendorId,
            ItemId = item.ItemId,
            VendorItemCode = item.VendorItemCode,
            VendorDescription = item.VendorDescription,
            Cost = item.Cost,
            LeadTimeDays = item.LeadTimeDays,
            MinimumOrderQuantity = item.MinimumOrderQuantity,
            IsActive = item.IsActive,
            IsPrimaryVendor = item.IsPrimaryVendor,
        };

        return Ok(ApiResponse<VendorItemDto>.Success(dto));
    }
}

public class VendorItemDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string? VendorItemCode { get; set; }
    public string? VendorDescription { get; set; }
    public decimal Cost { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool IsPrimaryVendor { get; set; }
}

public class CreateVendorItemRequest
{
    public Guid VendorId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string? VendorItemCode { get; set; }
    public string? VendorDescription { get; set; }
    public decimal Cost { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPrimaryVendor { get; set; }
}

public class UpdateVendorItemRequest
{
    public decimal Cost { get; set; }
    public DateTime EffectiveDate { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsPrimaryVendor { get; set; }
}
