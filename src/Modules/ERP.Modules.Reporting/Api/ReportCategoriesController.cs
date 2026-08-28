// <copyright file="ReportCategoriesController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using ERP.Modules.Reporting.Domain.Entities;
using ERP.Modules.Reporting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Reporting.Api;

[ApiController]
[Route("api/v1/reporting/categories")]
public class ReportCategoriesController : ControllerBase
{
    private readonly ReportingDbContext _db;

    public ReportCategoriesController(ReportingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid companyId)
    {
        var categories = await _db.ReportCategories
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var tree = BuildTree(categories, null);
        return Ok(ApiResponse<object>.Success(tree));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var category = await _db.ReportCategories.FindAsync(id);
        if (category == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Category not found" }));

        return Ok(ApiResponse<object>.Success(category));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReportCategoryCreateDto dto)
    {
        var category = new ReportCategory(
            dto.CompanyId,
            dto.Name,
            dto.ParentId,
            dto.SortOrder,
            dto.Description,
            dto.Icon);

        _db.ReportCategories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = category.Id },
            ApiResponse<object>.Success(category));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ReportCategoryUpdateDto dto)
    {
        var category = await _db.ReportCategories.FindAsync(id);
        if (category == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Category not found" }));

        category.Update(dto.Name, dto.ParentId, dto.SortOrder, dto.Description, dto.Icon);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Success(category));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _db.ReportCategories.FindAsync(id);
        if (category == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Category not found" }));

        var hasChildren = await _db.ReportCategories.AnyAsync(x => x.ParentId == id.ToString() && !x.DeletedOn.HasValue);
        if (hasChildren)
            return BadRequest(ApiResponse<object>.Failure(new[] { "Cannot delete category with children" }));

        var hasReports = await _db.ReportDefinitions.AnyAsync(x => x.Category == category.Name && !x.DeletedOn.HasValue);
        if (hasReports)
            return BadRequest(ApiResponse<object>.Failure(new[] { "Cannot delete category with assigned reports" }));

        category.MarkDeleted("system");
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Success(category));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var category = await _db.ReportCategories.FindAsync(id);
        if (category == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Category not found" }));

        category.Activate();
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Success(category));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var category = await _db.ReportCategories.FindAsync(id);
        if (category == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Category not found" }));

        category.Deactivate();
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Success(category));
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReportCategoryReorderDto dto)
    {
        foreach (var item in dto.Items)
        {
            var category = await _db.ReportCategories.FindAsync(item.Id);
            if (category != null)
            {
                category.Update(category.Name, category.ParentId, item.SortOrder, category.Description, category.Icon);
            }
        }

        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Success(new { message = "Reorder complete" }));
    }

    private static List<CategoryTreeNode> BuildTree(List<ReportCategory> all, string? parentId)
    {
        return all
            .Where(x => x.ParentId == parentId)
            .Select(x => new CategoryTreeNode
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Icon = x.Icon,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                Children = new Collection<CategoryTreeNode>(BuildTree(all, x.Id.ToString()))
            })
            .ToList();
    }
}

public class ReportCategoryCreateDto
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
}

public class ReportCategoryUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
}

public class ReportCategoryReorderDto
{
    public Collection<CategoryReorderItem> Items { get; } = new();
}

public class CategoryReorderItem
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
}

public class CategoryTreeNode
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
#pragma warning disable CA2227 // Collection properties should be read only
    public Collection<CategoryTreeNode> Children { get; set; } = new();
#pragma warning restore CA2227
}
