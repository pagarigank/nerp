// <copyright file="ItemCategoryController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Api;

#pragma warning disable S6960
[ApiController]
[Route("api/v1/inventory/item-categories")]
public class ItemCategoryController : ControllerBase
{
    private readonly IRepository<ItemCategory> _repository;
    private readonly InventoryDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ItemCategoryController(
        IRepository<ItemCategory> repository,
        InventoryDbContext context,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemCategoryDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.ItemCategories.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(c => c.CompanyId == companyId.Value);
        }

        var categories = await query.OrderBy(c => c.CategoryCode).ToListAsync(cancellationToken);

        var dtos = categories.Select(c => new ItemCategoryDto
        {
            Id = c.Id,
            CategoryCode = c.CategoryCode,
            Description = c.CategoryName,
            CompanyId = c.CompanyId,
            InventoryAccountId = c.InventoryAccountId ?? Guid.Empty,
            CogsAccountId = c.COGSAccountId ?? Guid.Empty,
            VarianceAccountId = c.VarianceAccountId ?? Guid.Empty,
        }).ToList();

        return Ok(ApiResponse<List<ItemCategoryDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);

        if (category == null)
        {
            return NotFound(ApiResponse<ItemCategoryDto>.Failure(["Category not found."]));
        }

        var dto = new ItemCategoryDto
        {
            Id = category.Id,
            CategoryCode = category.CategoryCode,
            Description = category.CategoryName,
            CompanyId = category.CompanyId,
            InventoryAccountId = category.InventoryAccountId ?? Guid.Empty,
            CogsAccountId = category.COGSAccountId ?? Guid.Empty,
            VarianceAccountId = category.VarianceAccountId ?? Guid.Empty,
        };

        return Ok(ApiResponse<ItemCategoryDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> Create(
        [FromBody] CreateItemCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = new ItemCategory(
            request.CategoryCode,
            request.Description,
            request.CompanyId,
            request.InventoryAccountId,
            request.CogsAccountId,
            request.VarianceAccountId);

        await _repository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ItemCategoryDto
        {
            Id = category.Id,
            CategoryCode = category.CategoryCode,
            Description = category.CategoryName,
            CompanyId = category.CompanyId,
            InventoryAccountId = category.InventoryAccountId ?? Guid.Empty,
            CogsAccountId = category.COGSAccountId ?? Guid.Empty,
            VarianceAccountId = category.VarianceAccountId ?? Guid.Empty,
        };

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, ApiResponse<ItemCategoryDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> Update(
        Guid id,
        [FromBody] UpdateItemCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);

        if (category == null)
        {
            return NotFound(ApiResponse<ItemCategoryDto>.Failure(["Category not found."]));
        }

        category.UpdateAccounts(
            request.InventoryAccountId,
            request.CogsAccountId,
            request.VarianceAccountId);

        _repository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ItemCategoryDto
        {
            Id = category.Id,
            CategoryCode = category.CategoryCode,
            Description = category.CategoryName,
            CompanyId = category.CompanyId,
            InventoryAccountId = category.InventoryAccountId ?? Guid.Empty,
            CogsAccountId = category.COGSAccountId ?? Guid.Empty,
            VarianceAccountId = category.VarianceAccountId ?? Guid.Empty,
        };

        return Ok(ApiResponse<ItemCategoryDto>.Success(dto));
    }
}

public class ItemCategoryDto
{
    public Guid Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid InventoryAccountId { get; set; }
    public Guid CogsAccountId { get; set; }
    public Guid VarianceAccountId { get; set; }
}

public class CreateItemCategoryRequest
{
    public string CategoryCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid? InventoryAccountId { get; set; }
    public Guid? CogsAccountId { get; set; }
    public Guid? VarianceAccountId { get; set; }
}

public class UpdateItemCategoryRequest
{
    public string Description { get; set; } = string.Empty;
    public Guid? InventoryAccountId { get; set; }
    public Guid? CogsAccountId { get; set; }
    public Guid? VarianceAccountId { get; set; }
}
