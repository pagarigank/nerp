// <copyright file="TaxDeterminationRuleController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using ERP.Modules.Platform.Api.Authorization;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.MalaysianTax.Api;

/// <summary>
/// CRUD API for tax determination rules.
/// Maps (item_category, customer_type, place_of_supply) → tax_code for automated SST assignment.
/// All endpoints enforce multi-company isolation via ICurrentUserService.
/// System-wide rules (CompanyId = Guid.Empty) are visible to all; company-specific rules are scoped.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class TaxDeterminationRuleController : ControllerBase
{
    private readonly ITaxDeterminationRuleRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public TaxDeterminationRuleController(ITaxDeterminationRuleRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Gets all active tax determination rules for a company (including system-wide defaults).
    /// Company-scoped users only see rules for their allowed companies.
    /// </summary>
    [HttpGet]
    [RequirePermission("tax", "tax-rules", "view")]
    [ProducesResponseType(typeof(IEnumerable<TaxDeterminationRule>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId)
    {
        // If a specific company is requested, validate access
        if (companyId != Guid.Empty && !_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(companyId))
            return Forbid();

        var targetCompanyId = companyId != Guid.Empty ? companyId : Guid.Empty;
        var rules = await _repository.GetAllActiveAsync(targetCompanyId);

        // Filter to user's companies (system-wide rules pass through)
        var filtered = _currentUser.IsSuperAdmin
            ? rules
            : rules.Where(r => r.CompanyId == Guid.Empty || _currentUser.CompanyIds.Contains(r.CompanyId));

        return Ok(filtered);
    }

    /// <summary>
    /// Gets a tax determination rule by ID (must be system-wide or belong to caller's company).
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("tax", "tax-rules", "view")]
    [ProducesResponseType(typeof(TaxDeterminationRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule == null)
            return NotFound();
        if (!_currentUser.IsSuperAdmin && rule.CompanyId != Guid.Empty && !_currentUser.CompanyIds.Contains(rule.CompanyId))
            return Forbid();
        return Ok(rule);
    }

    /// <summary>
    /// Preview which tax code would be determined for a given context (dry-run).
    /// </summary>
    [HttpGet("determine")]
    [RequirePermission("tax", "tax-rules", "view")]
    [ProducesResponseType(typeof(TaxDeterminationPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Determine(
        [FromQuery] Guid companyId,
        [FromQuery] string itemCategory,
        [FromQuery] string customerType,
        [FromQuery] string placeOfSupply)
    {
        if (companyId != Guid.Empty && !_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(companyId))
            return Forbid();

        var rules = await _repository.FindMatchingRulesAsync(
            companyId,
            itemCategory ?? "*",
            customerType ?? "*",
            placeOfSupply ?? "*",
            DateTime.UtcNow);

        var ruleList = rules.ToList();
        if (ruleList.Count == 0)
            return NotFound(new { Message = "No matching tax determination rule found for this context." });

        var best = ruleList[0];
        return Ok(new TaxDeterminationPreview
        {
            MatchedRuleId = best.Id,
            ItemCategory = best.ItemCategory,
            CustomerType = best.CustomerType,
            PlaceOfSupply = best.PlaceOfSupply,
            TaxCodeId = best.TaxCodeId,
            Priority = best.Priority,
            SpecificityScore = best.SpecificityScore(),
            TotalMatchingRules = ruleList.Count
        });
    }

    /// <summary>
    /// Creates a new tax determination rule. Caller must have access to the target company.
    /// </summary>
    [HttpPost]
    [RequirePermission("tax", "tax-rules", "create")]
    [ProducesResponseType(typeof(TaxDeterminationRule), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] TaxDeterminationRuleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.TaxCodeId == Guid.Empty)
            return BadRequest("TaxCodeId is required.");

        // Validate company access (system-wide rules only for super admin)
        if (request.CompanyId != Guid.Empty && !_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(request.CompanyId))
            return Forbid();
        if (request.CompanyId == Guid.Empty && !_currentUser.IsSuperAdmin)
            return Forbid("Only super admins can create system-wide rules.");

        var entity = new TaxDeterminationRule();
        entity.Update(
            request.CompanyId,
            request.ItemCategory ?? "*",
            request.CustomerType ?? "*",
            request.PlaceOfSupply ?? "*",
            request.TaxCodeId,
            request.Priority,
            request.IsActive,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.Description);

        await _repository.AddAsync(entity);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
    }

    /// <summary>
    /// Updates a tax determination rule (must be system-wide or belong to caller's company).
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("tax", "tax-rules", "edit")]
    [ProducesResponseType(typeof(TaxDeterminationRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaxDeterminationRuleRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();
        if (!_currentUser.IsSuperAdmin && existing.CompanyId != Guid.Empty && !_currentUser.CompanyIds.Contains(existing.CompanyId))
            return Forbid();

        existing.Update(
            request.CompanyId,
            request.ItemCategory ?? "*",
            request.CustomerType ?? "*",
            request.PlaceOfSupply ?? "*",
            request.TaxCodeId,
            request.Priority,
            request.IsActive,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.Description);

        await _repository.UpdateAsync(existing);
        return Ok(existing);
    }

    /// <summary>
    /// Soft-deletes a tax determination rule (must be system-wide or belong to caller's company).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("tax", "tax-rules", "delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NoContent();
        if (!_currentUser.IsSuperAdmin && existing.CompanyId != Guid.Empty && !_currentUser.CompanyIds.Contains(existing.CompanyId))
            return Forbid();

        await _repository.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>
/// Request body for creating or updating a tax determination rule.
/// </summary>
public class TaxDeterminationRuleRequest
{
    /// <summary>Gets or sets the company ID (Guid.Empty for system-wide default).</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Gets or sets the item category to match ("*" for any).</summary>
    [MaxLength(50)]
    public string? ItemCategory { get; set; }

    /// <summary>Gets or sets the customer type to match ("*" for any).</summary>
    [MaxLength(50)]
    public string? CustomerType { get; set; }

    /// <summary>Gets or sets the place of supply to match ("*" for any).</summary>
    [MaxLength(50)]
    public string? PlaceOfSupply { get; set; }

    /// <summary>Gets or sets the tax code ID this rule resolves to.</summary>
    [Required]
    public Guid TaxCodeId { get; set; }

    /// <summary>Gets or sets the priority (lower = evaluated first).</summary>
    public int Priority { get; set; } = 100;

    /// <summary>Gets or sets a value indicating whether this rule is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the effective-from date.</summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>Gets or sets the effective-to date.</summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Gets or sets a description of when this rule applies.</summary>
    [MaxLength(250)]
    public string? Description { get; set; }
}

/// <summary>
/// Preview result showing which rule matched for a given determination context.
/// </summary>
public class TaxDeterminationPreview
{
    /// <summary>Gets or sets the matched rule ID.</summary>
    public Guid MatchedRuleId { get; set; }

    /// <summary>Gets or sets the matched item category pattern.</summary>
    public string ItemCategory { get; set; } = string.Empty;

    /// <summary>Gets or sets the matched customer type pattern.</summary>
    public string CustomerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the matched place of supply pattern.</summary>
    public string PlaceOfSupply { get; set; } = string.Empty;

    /// <summary>Gets or sets the resolved tax code ID.</summary>
    public Guid TaxCodeId { get; set; }

    /// <summary>Gets or sets the rule priority.</summary>
    public int Priority { get; set; }

    /// <summary>Gets or sets the specificity score.</summary>
    public int SpecificityScore { get; set; }

    /// <summary>Gets or sets the total number of matching rules.</summary>
    public int TotalMatchingRules { get; set; }
}
