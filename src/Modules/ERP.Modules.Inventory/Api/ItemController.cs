// <copyright file="ItemController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/inventory/items")]
public class ItemController : ControllerBase
{
    private readonly IRepository<Item> _repository;
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ItemController(
        IRepository<Item> repository,
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] ItemStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.Items.AsQueryable();

        query = query.ApplyCompanyScope(HttpContext, i => i.CompanyId, companyId);

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        var items = await query.ToListAsync(cancellationToken);

        var dtos = items.Select(i => new ItemDto
        {
            Id = i.Id,
            ItemCode = i.ItemCode,
            Description = i.Description,
            LongDescription = i.LongDescription,
            ItemType = i.ItemType.ToString(),
            BaseUnitOfMeasure = i.BaseUnitOfMeasure,
            CostingMethod = i.CostingMethod.ToString(),
            Status = i.Status.ToString(),
            StandardCost = i.StandardCost,
            ReorderPoint = i.ReorderPoint,
            ReorderQuantity = i.ReorderQuantity,
            SafetyStock = i.SafetyStock,
            LeadTimeDays = i.LeadTimeDays,
            IsLotControlled = i.IsLotControlled,
            IsSerialControlled = i.IsSerialControlled,
            IsKit = i.IsKit,
        }).ToList();

        return Ok(ApiResponse<List<ItemDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);

        if (item == null)
        {
            return NotFound(ApiResponse<ItemDto>.Failure(["Item not found."]));
        }

        var dto = new ItemDto
        {
            Id = item.Id,
            ItemCode = item.ItemCode,
            Description = item.Description,
            LongDescription = item.LongDescription,
            ItemType = item.ItemType.ToString(),
            BaseUnitOfMeasure = item.BaseUnitOfMeasure,
            CostingMethod = item.CostingMethod.ToString(),
            Status = item.Status.ToString(),
            StandardCost = item.StandardCost,
            ReorderPoint = item.ReorderPoint,
            ReorderQuantity = item.ReorderQuantity,
            SafetyStock = item.SafetyStock,
            LeadTimeDays = item.LeadTimeDays,
            Weight = item.Weight,
            Length = item.Length,
            Width = item.Width,
            Height = item.Height,
            WeightUnit = item.WeightUnit,
            IsHazardousMaterial = item.IsHazardousMaterial,
            HazardClass = item.HazardClass,
            CountryOfOrigin = item.CountryOfOrigin,
            HsCode = item.HsCode,
            StorageCondition = item.StorageCondition,
            IsKit = item.IsKit,
        };

        return Ok(ApiResponse<ItemDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ItemDto>>> Create(
        [FromBody] CreateItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = new Item(
            request.ItemCode,
            request.Description,
            request.CompanyId,
            request.ItemType,
            request.BaseUnitOfMeasure,
            request.CostingMethod,
            request.ItemCategoryId);

        if (!string.IsNullOrEmpty(request.LongDescription))
        {
            item.UpdateDescription(request.Description, request.LongDescription);
        }

        if (request.StandardCost.HasValue)
        {
            item.UpdateStandardCost(request.StandardCost.Value);
        }

        item.UpdateReorderParameters(
            request.ReorderPoint,
            request.ReorderQuantity,
            request.SafetyStock,
            request.LeadTimeDays);

        item.UpdatePhysicalAttributes(
            request.Weight,
            request.Length,
            request.Width,
            request.Height,
            request.WeightUnit,
            request.IsHazardousMaterial,
            request.HazardClass,
            request.CountryOfOrigin,
            request.HsCode,
            request.StorageCondition);

        if (request.IsKit)
        {
            item.SetKit(true);
        }

        await _repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ItemDto
        {
            Id = item.Id,
            ItemCode = item.ItemCode,
            Description = item.Description,
            ItemType = item.ItemType.ToString(),
            BaseUnitOfMeasure = item.BaseUnitOfMeasure,
            Status = item.Status.ToString(),
            StandardCost = item.StandardCost,
        };

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ApiResponse<ItemDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemDto>>> Update(
        Guid id, [FromBody] UpdateItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound(ApiResponse<ItemDto>.Failure(["Item not found."]));

        if (request.Description is not null)
        {
            item.UpdateDescription(request.Description, request.LongDescription);
        }

        if (request.StandardCost.HasValue)
        {
            item.UpdateStandardCost(request.StandardCost.Value);
        }

        item.UpdateReorderParameters(
            request.ReorderPoint,
            request.ReorderQuantity,
            request.SafetyStock,
            request.LeadTimeDays);

        item.UpdatePhysicalAttributes(
            request.Weight,
            request.Length,
            request.Width,
            request.Height,
            request.WeightUnit,
            request.IsHazardousMaterial,
            request.HazardClass,
            request.CountryOfOrigin,
            request.HsCode,
            request.StorageCondition);

        if (request.IsKit.HasValue)
        {
            item.SetKit(request.IsKit.Value);
        }

        if (request.IsLotControlled.HasValue)
        {
            item.SetLotControlled(request.IsLotControlled.Value);
        }

        if (request.IsSerialControlled.HasValue)
        {
            item.SetSerialControlled(request.IsSerialControlled.Value);
        }

        if (request.Status.HasValue)
        {
            switch (request.Status.Value)
            {
                case ItemStatus.Active:
                    item.Activate();
                    break;
                case ItemStatus.Inactive:
                    item.Deactivate();
                    break;
                case ItemStatus.Discontinued:
                    item.Discontinue();
                    break;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ItemDto
        {
            Id = item.Id,
            ItemCode = item.ItemCode,
            Description = item.Description,
            LongDescription = item.LongDescription,
            ItemType = item.ItemType.ToString(),
            BaseUnitOfMeasure = item.BaseUnitOfMeasure,
            CostingMethod = item.CostingMethod.ToString(),
            Status = item.Status.ToString(),
            StandardCost = item.StandardCost,
            ReorderPoint = item.ReorderPoint,
            ReorderQuantity = item.ReorderQuantity,
            SafetyStock = item.SafetyStock,
            LeadTimeDays = item.LeadTimeDays,
            Weight = item.Weight,
            Length = item.Length,
            Width = item.Width,
            Height = item.Height,
            WeightUnit = item.WeightUnit,
            IsHazardousMaterial = item.IsHazardousMaterial,
            HazardClass = item.HazardClass,
            CountryOfOrigin = item.CountryOfOrigin,
            HsCode = item.HsCode,
            StorageCondition = item.StorageCondition,
            IsKit = item.IsKit,
        };

        return Ok(ApiResponse<ItemDto>.Success(dto));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound(ApiResponse<string>.Failure(["Item not found."]));

        item.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<string>.Success("Item deactivated."));
    }
}

public class ItemDto
{
    public Guid Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LongDescription { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string BaseUnitOfMeasure { get; set; } = string.Empty;
    public string? CostingMethod { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? StandardCost { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? ReorderQuantity { get; set; }
    public decimal? SafetyStock { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? WeightUnit { get; set; }
    public bool IsHazardousMaterial { get; set; }
    public string? HazardClass { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? HsCode { get; set; }
    public string? StorageCondition { get; set; }
    public bool IsKit { get; set; }
    public bool IsLotControlled { get; set; }
    public bool IsSerialControlled { get; set; }
    public string? ABCClass { get; set; }
}

public class UpdateItemRequest
{
    public string? Description { get; set; }
    public string? LongDescription { get; set; }
    public decimal? StandardCost { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? ReorderQuantity { get; set; }
    public decimal? SafetyStock { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? WeightUnit { get; set; }
    public bool IsHazardousMaterial { get; set; }
    public string? HazardClass { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? HsCode { get; set; }
    public string? StorageCondition { get; set; }
    public bool? IsKit { get; set; }
    public bool? IsLotControlled { get; set; }
    public bool? IsSerialControlled { get; set; }
    public ItemStatus? Status { get; set; }
}

public class CreateItemRequest
{
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LongDescription { get; set; }
    public Guid CompanyId { get; set; }
    public ItemType ItemType { get; set; }
    public string BaseUnitOfMeasure { get; set; } = string.Empty;
    public CostingMethod CostingMethod { get; set; }
    public Guid ItemCategoryId { get; set; }
    public decimal? StandardCost { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? ReorderQuantity { get; set; }
    public decimal? SafetyStock { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? WeightUnit { get; set; }
    public bool IsHazardousMaterial { get; set; }
    public string? HazardClass { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? HsCode { get; set; }
    public string? StorageCondition { get; set; }
    public bool IsKit { get; set; }
    public bool IsLotControlled { get; set; }
    public bool IsSerialControlled { get; set; }
}
