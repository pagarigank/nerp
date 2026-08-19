// <copyright file="BomHeaderController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.BillOfMaterials.Domain.Entities;
using ERP.Modules.BillOfMaterials.Domain.Services;
using ERP.Modules.BillOfMaterials.Infrastructure;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.BillOfMaterials.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/bom/bom-headers")]
public class BomHeaderController : ControllerBase
{
    private readonly BomDbContext _context;
    private readonly InventoryDbContext _invContext;
    private readonly IBomUnitOfWork _unitOfWork;
    private readonly RequisitionSuggester _requisitionSuggester;
    private readonly ICurrentUserService _currentUser;

    public BomHeaderController(BomDbContext context, InventoryDbContext invContext, IBomUnitOfWork unitOfWork, RequisitionSuggester requisitionSuggester, ICurrentUserService currentUser)
    {
        _context = context;
        _invContext = invContext;
        _unitOfWork = unitOfWork;
        _requisitionSuggester = requisitionSuggester;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BomHeaderDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] BomStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.BomHeaders
            .Include(h => h.Components)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(h => h.CompanyId == companyId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(h => h.Status == status.Value);
        }

        var headers = await query.OrderByDescending(h => h.CreatedOn).ToListAsync(cancellationToken);
        var dtos = headers.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<BomHeaderDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BomHeaderDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders
            .Include(h => h.Components)
            .Include(h => h.Revisions)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (header is null)
        {
            return NotFound(ApiResponse<BomHeaderDto>.Failure(new[] { "BOM header not found." }, 404));
        }

        var dto = MapToDto(header);
        return Ok(ApiResponse<BomHeaderDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateBomHeaderRequest request,
        CancellationToken cancellationToken)
    {
        var bomType = Enum.TryParse<BomType>(request.BomType, true, out var parsed) ? parsed : BomType.Standard;

        var header = new BomHeader(
            request.CompanyId,
            request.ParentItemId,
            request.Revision,
            bomType,
            BomStatus.Draft,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.Description,
            request.YieldPercentage,
            request.AlternateCode);

        _context.BomHeaders.Add(header);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Add revision history entry
        var revision = new BomRevisionHistory(
            header.Id, header.Revision, "Initial BOM creation", null, header.EffectiveFrom);
        _context.BomRevisionHistories.Add(revision);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(header.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id,
        [FromBody] UpdateBomHeaderRequest request,
        CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders.FindAsync(new object[] { id }, cancellationToken);
        if (header is null)
        {
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));
        }

        var updateBomType = request.BomType is not null && Enum.TryParse<BomType>(request.BomType, true, out var bt) ? bt : (BomType?)null;
        var updateStatus = request.Status is not null && Enum.TryParse<BomStatus>(request.Status, true, out var st) ? st : (BomStatus?)null;

        header.Update(
            request.Description,
            updateBomType,
            updateStatus,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.YieldPercentage,
            request.AlternateCode);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders.FindAsync(new object[] { id }, cancellationToken);
        if (header is null)
        {
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));
        }

        if (header.Status == BomStatus.Active)
        {
            return BadRequest(ApiResponse.Failure(new[] { "Cannot delete an active BOM. Set it to Obsolete first." }));
        }

        _context.BomHeaders.Remove(header);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- Component Lines ---
    [HttpGet("{id:guid}/components")]
    public async Task<ActionResult<ApiResponse<List<BomComponentLineDto>>>> GetComponents(
        Guid id,
        CancellationToken cancellationToken)
    {
        var lines = await _context.BomComponentLines
            .Where(c => c.BomHeaderId == id)
            .OrderBy(c => c.OperationSequence)
            .ToListAsync(cancellationToken);

        var dtos = lines.Select(MapComponentToDto).ToList();
        return Ok(ApiResponse<List<BomComponentLineDto>>.Success(dtos));
    }

    [HttpPost("{id:guid}/components")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddComponent(
        Guid id,
        [FromBody] AddComponentRequest request,
        CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders.FindAsync(new object[] { id }, cancellationToken);
        if (header is null)
        {
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));
        }

        var line = header.AddComponent(
            request.ComponentItemId,
            request.QuantityPerParent,
            request.UnitOfMeasure,
            request.ScrapFactor,
            request.OperationSequence,
            request.WorkCenterId,
            request.IsPhantom,
            request.IsCritical,
            request.Notes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(line.Id));
    }

    [HttpPut("{id:guid}/components/{lineId:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateComponent(
        Guid id,
        Guid lineId,
        [FromBody] UpdateComponentRequest request,
        CancellationToken cancellationToken)
    {
        var line = await _context.BomComponentLines
            .FirstOrDefaultAsync(c => c.Id == lineId && c.BomHeaderId == id, cancellationToken);

        if (line is null)
        {
            return NotFound(ApiResponse.Failure(new[] { "Component line not found." }, 404));
        }

        line.Update(
            request.QuantityPerParent,
            request.UnitOfMeasure,
            request.ScrapFactor,
            request.OperationSequence,
            request.WorkCenterId,
            request.IsPhantom,
            request.IsCritical,
            request.Notes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpDelete("{id:guid}/components/{lineId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteComponent(
        Guid id,
        Guid lineId,
        CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders.FindAsync(new object[] { id }, cancellationToken);
        if (header is null)
        {
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));
        }

        header.RemoveComponent(lineId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- BOM Explosion ---
    [HttpGet("{id:guid}/explode")]
    public async Task<ActionResult<ApiResponse<List<BomExplosionDto>>>> Explode(
        Guid id,
        [FromQuery] decimal quantity = 1,
        [FromQuery] int maxLevel = 10,
        CancellationToken cancellationToken = default)
    {
        var result = new List<BomExplosionDto>();
        await ExplodeLevel(id, quantity, 0, maxLevel, result, cancellationToken);
        return Ok(ApiResponse<List<BomExplosionDto>>.Success(result));
    }

    private async Task ExplodeLevel(
        Guid bomHeaderId,
        decimal parentQty,
        int currentLevel,
        int maxLevel,
        List<BomExplosionDto> result,
        CancellationToken ct)
    {
        if (currentLevel >= maxLevel)
        {
            return;
        }

        var header = await _context.BomHeaders
            .Include(h => h.Components)
            .FirstOrDefaultAsync(h => h.Id == bomHeaderId, ct);

        if (header is null)
        {
            return;
        }

        foreach (var comp in header.Components)
        {
            var netQty = comp.EffectiveQuantity * parentQty;
            result.Add(new BomExplosionDto
            {
                Level = currentLevel,
                ComponentItemId = comp.ComponentItemId,
                QuantityPerParent = comp.QuantityPerParent,
                NetQuantity = netQty,
                UnitOfMeasure = comp.UnitOfMeasure,
                ScrapFactor = comp.ScrapFactor,
                IsPhantom = comp.IsPhantom,
                IsCritical = comp.IsCritical,
                OperationSequence = comp.OperationSequence,
            });

            // If component is a phantom, explode its sub-BOM too
            if (comp.IsPhantom)
            {
                var subBom = await _context.BomHeaders
                    .FirstOrDefaultAsync(
                        h => h.ParentItemId == comp.ComponentItemId && h.Status == BomStatus.Active,
                        ct);

                if (subBom is not null)
                {
                    await ExplodeLevel(subBom.Id, netQty, currentLevel + 1, maxLevel, result, ct);
                }
            }
        }
    }

    // --- Where Used ---
    [HttpGet("where-used")]
    public async Task<ActionResult<ApiResponse<List<BomWhereUsedDto>>>> WhereUsed(
        [FromQuery] Guid componentItemId,
        CancellationToken cancellationToken)
    {
        var results = await _context.BomComponentLines
            .Where(c => c.ComponentItemId == componentItemId)
            .Join(
                _context.BomHeaders,
                c => c.BomHeaderId,
                h => h.Id,
                (c, h) => new BomWhereUsedDto
                {
                    BomHeaderId = h.Id,
                    ParentItemId = h.ParentItemId,
                    Revision = h.Revision,
                    QuantityPerParent = c.QuantityPerParent,
                    UnitOfMeasure = c.UnitOfMeasure,
                    IsPhantom = c.IsPhantom,
                    OperationSequence = c.OperationSequence,
                })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<BomWhereUsedDto>>.Success(results));
    }

    // --- Cost Roll-Up ---
    [HttpGet("{id:guid}/cost-rollup")]
    public async Task<ActionResult<ApiResponse<BomCostRollupDto>>> CostRollup(
        Guid id,
        CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders
            .Include(h => h.Components)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (header is null)
        {
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));
        }

        var dto = new BomCostRollupDto
        {
            BomHeaderId = header.Id,
            ParentItemId = header.ParentItemId,
            Revision = header.Revision,
            YieldPercentage = header.YieldPercentage,
            Components = [],
        };

        decimal totalMaterialCost = 0;

        foreach (var comp in header.Components)
        {
            // Look up item standard cost
            var itemCost = await _invContext.Items
                .Where(i => i.Id == comp.ComponentItemId)
                .Select(i => i.StandardCost)
                .FirstOrDefaultAsync(cancellationToken);

            var unitCost = itemCost ?? 0;
            var extCost = comp.EffectiveQuantity * unitCost;
            totalMaterialCost += extCost;

            dto.Components.Add(new BomCostRollupLineDto
            {
                ComponentItemId = comp.ComponentItemId,
                QuantityPerParent = comp.QuantityPerParent,
                EffectiveQuantity = comp.EffectiveQuantity,
                UnitCost = unitCost,
                ExtendedCost = extCost,
                ScrapFactor = comp.ScrapFactor,
            });
        }

        // Adjust for yield
        if (header.YieldPercentage > 0 && header.YieldPercentage != 100)
        {
            totalMaterialCost = totalMaterialCost / (header.YieldPercentage / 100m);
        }

        dto.TotalMaterialCost = totalMaterialCost;
        dto.TotalCost = totalMaterialCost;

        // Update estimated costs on header
        header.UpdateEstimatedCosts(totalMaterialCost, 0, 0);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<BomCostRollupDto>.Success(dto));
    }

    // --- Cost Roll-Up to Item Standard Cost (cross-module wiring) ---
    [HttpPost("{id:guid}/apply-cost-to-item")]
    public async Task<ActionResult<ApiResponse>> ApplyCostToItem(
        Guid id, CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders
            .Include(h => h.Components)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (header is null)
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));

        var totalMaterialCost = 0m;
        foreach (var comp in header.Components)
        {
            var unitCost = await _invContext.Items
                .Where(i => i.Id == comp.ComponentItemId)
                .Select(i => i.StandardCost ?? 0)
                .FirstOrDefaultAsync(cancellationToken);
            totalMaterialCost += comp.EffectiveQuantity * unitCost;
        }

        if (header.YieldPercentage > 0 && header.YieldPercentage != 100)
            totalMaterialCost = totalMaterialCost / (header.YieldPercentage / 100m);

        var item = await _invContext.Items.FindAsync(new object[] { header.ParentItemId }, cancellationToken);
        if (item is null)
            return NotFound(ApiResponse.Failure(new[] { "Parent item not found." }, 404));

        item.UpdateStandardCost(totalMaterialCost);
        await _invContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<decimal>.Success(totalMaterialCost));
    }

    // --- What-if Cost Simulation (675) ---
    [HttpPost("{id:guid}/what-if")]
    public async Task<ActionResult<ApiResponse<WhatIfSimulationDto>>> WhatIfSimulation(
        Guid id,
        [FromBody] WhatIfRequest request,
        CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders
            .Include(h => h.Components)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (header is null)
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));

        // Current cost roll-up (using item standard costs).
        decimal currentCost = 0;
        foreach (var comp in header.Components)
        {
            var unitCost = await _invContext.Items
                .Where(i => i.Id == comp.ComponentItemId)
                .Select(i => i.StandardCost ?? 0)
                .FirstOrDefaultAsync(cancellationToken);
            currentCost += comp.EffectiveQuantity * unitCost;
        }

        // Apply overrides.
        decimal simulatedCost = 0;
        var overrideMap = request.Overrides?
            .Where(o => o.ComponentLineId != Guid.Empty)
            .ToDictionary(o => o.ComponentLineId) ?? new Dictionary<Guid, WhatIfComponentOverride>();

        foreach (var comp in header.Components)
        {
            var qty = comp.QuantityPerParent;
            var unit = comp.EstimatedUnitCost
                ?? await _invContext.Items.Where(i => i.Id == comp.ComponentItemId)
                    .Select(i => i.StandardCost ?? 0).FirstOrDefaultAsync(cancellationToken);

            if (overrideMap.TryGetValue(comp.Id, out var ov))
            {
                if (ov.UnitCost.HasValue)
                    unit = ov.UnitCost.Value;
                if (ov.QuantityPerParent.HasValue)
                    qty = ov.QuantityPerParent.Value;
            }

            var scrap = comp.ScrapFactor;
            var effective = qty * (1m + (scrap / 100m));
            simulatedCost += effective * unit;
        }

        if (header.YieldPercentage > 0 && header.YieldPercentage != 100)
            simulatedCost = simulatedCost / (header.YieldPercentage / 100m);

        return Ok(ApiResponse<WhatIfSimulationDto>.Success(new WhatIfSimulationDto
        {
            BomHeaderId = header.Id,
            CurrentMaterialCost = currentCost,
            SimulatedMaterialCost = simulatedCost,
            Delta = simulatedCost - currentCost,
            PercentChange = currentCost != 0 ? (simulatedCost - currentCost) / currentCost * 100 : null,
            AppliedOverrides = overrideMap.Count,
        }));
    }

    // --- BOM -> Requisition suggestion (676 / 708) ---
    [HttpPost("{id:guid}/suggest-requisitions")]
    public async Task<ActionResult<ApiResponse<RequisitionSuggestionResult>>> SuggestRequisitions(
        Guid id,
        [FromBody] SuggestRequisitionsRequest request,
        CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders.FindAsync(new object[] { id }, cancellationToken);
        if (header is null)
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));

        var requestorId = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : Guid.Empty;
        var result = await _requisitionSuggester.SuggestAsync(
            id, request.PlannedQuantity, header.CompanyId, requestorId, cancellationToken);

        return Ok(ApiResponse<RequisitionSuggestionResult>.Success(result));
    }

    // --- Component Substitutions ---
    [HttpGet("{id:guid}/substitutions")]
    public async Task<ActionResult<ApiResponse<List<BomSubstitutionDto>>>> GetSubstitutions(
        Guid id, CancellationToken cancellationToken)
    {
        var subs = await _context.BomComponentSubstitutions
            .Where(s => s.BomHeaderId == id)
            .OrderBy(s => s.Priority)
            .ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<BomSubstitutionDto>>.Success(subs.Select(MapSubstitution).ToList()));
    }

    [HttpPost("{id:guid}/substitutions")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddSubstitution(
        Guid id, [FromBody] AddSubstitutionRequest request, CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders.FindAsync(new object[] { id }, cancellationToken);
        if (header is null)
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));

        var line = await _context.BomComponentLines
            .FirstOrDefaultAsync(c => c.Id == request.ComponentLineId && c.BomHeaderId == id, cancellationToken);
        if (line is null)
            return NotFound(ApiResponse.Failure(new[] { "Component line not found." }, 404));

        var sub = header.AddSubstitution(request.ComponentLineId, request.SubstituteItemId, request.Reason, request.CostVariance, request.Priority);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(sub.Id));
    }

    [HttpPost("{id:guid}/substitutions/{subId:guid}/approve")]
    public async Task<ActionResult<ApiResponse>> ApproveSubstitution(
        Guid id, Guid subId, CancellationToken cancellationToken)
    {
        var sub = await _context.BomComponentSubstitutions
            .FirstOrDefaultAsync(s => s.Id == subId && s.BomHeaderId == id, cancellationToken);
        if (sub is null)
            return NotFound(ApiResponse.Failure(new[] { "Substitution not found." }, 404));
        sub.Approve();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- Component Allocation ---
    [HttpGet("{id:guid}/allocations")]
    public async Task<ActionResult<ApiResponse<List<BomAllocationDto>>>> GetAllocations(
        Guid id, CancellationToken cancellationToken)
    {
        var allocs = await _context.ComponentAllocations
            .Where(a => a.BomHeaderId == id)
            .ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<BomAllocationDto>>.Success(allocs.Select(MapAllocation).ToList()));
    }

    [HttpPost("{id:guid}/allocations")]
    public async Task<ActionResult<ApiResponse<Guid>>> AllocateComponent(
        Guid id, [FromBody] AllocateComponentRequest request, CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders.FindAsync(new object[] { id }, cancellationToken);
        if (header is null)
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));

        var alloc = header.AddAllocation(request.BuildOrderId, request.ComponentItemId, request.Quantity, request.UnitOfMeasure, request.WarehouseId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(alloc.Id));
    }

    [HttpPut("{id:guid}/allocations/{allocId:guid}/release")]
    public async Task<ActionResult<ApiResponse>> ReleaseAllocation(
        Guid id, Guid allocId, CancellationToken cancellationToken)
    {
        var alloc = await _context.ComponentAllocations
            .FirstOrDefaultAsync(a => a.Id == allocId && a.BomHeaderId == id, cancellationToken);
        if (alloc is null)
            return NotFound(ApiResponse.Failure(new[] { "Allocation not found." }, 404));
        alloc.Release();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- Engineering Change Notices ---
    [HttpGet("{id:guid}/ecns")]
    public async Task<ActionResult<ApiResponse<List<BomEcnDto>>>> GetEcns(
        Guid id, CancellationToken cancellationToken)
    {
        var ecns = await _context.EngineeringChangeNotices
            .Where(e => e.BomHeaderId == id)
            .OrderByDescending(e => e.CreatedOn)
            .ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<BomEcnDto>>.Success(ecns.Select(MapEcn).ToList()));
    }

    [HttpPost("{id:guid}/ecns")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateEcn(
        Guid id, [FromBody] CreateEcnRequest request, CancellationToken cancellationToken)
    {
        var header = await _context.BomHeaders.FindAsync(new object[] { id }, cancellationToken);
        if (header is null)
            return NotFound(ApiResponse.Failure(new[] { "BOM header not found." }, 404));

        var ecn = header.AddEngineeringChangeNotice(request.CompanyId, request.EcnNumber, request.Title, request.Description, request.PlannedEffectivity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(ecn.Id));
    }

    [HttpPut("{id:guid}/ecns/{ecnId:guid}/transition")]
    public async Task<ActionResult<ApiResponse>> TransitionEcn(
        Guid id, Guid ecnId, [FromBody] EcnTransitionRequest request, CancellationToken cancellationToken)
    {
        var ecn = await _context.EngineeringChangeNotices
            .FirstOrDefaultAsync(e => e.Id == ecnId && e.BomHeaderId == id, cancellationToken);
        if (ecn is null)
            return NotFound(ApiResponse.Failure(new[] { "ECN not found." }, 404));

        switch (request.Action?.ToUpperInvariant())
        {
            case "SUBMIT": ecn.Submit(request.Reviewer ?? "system"); break;
            case "REVIEW": ecn.StartReview(); break;
            case "APPROVE": ecn.Approve(request.Approver ?? "system"); break;
            case "REJECT": ecn.Reject(request.Reason ?? "rejected"); break;
            case "EXECUTE": ecn.Execute(request.Effectivity ?? DateTime.UtcNow); break;
            default: return BadRequest(ApiResponse.Failure(new[] { "Unknown ECN action." }));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    // --- BOM Comparison (two revisions) ---
    [HttpGet("compare")]
    public async Task<ActionResult<ApiResponse<BomComparisonDto>>> Compare(
        [FromQuery] Guid bomA, [FromQuery] Guid bomB, CancellationToken cancellationToken)
    {
        var a = await _context.BomHeaders.Include(h => h.Components).FirstOrDefaultAsync(h => h.Id == bomA, cancellationToken);
        var b = await _context.BomHeaders.Include(h => h.Components).FirstOrDefaultAsync(h => h.Id == bomB, cancellationToken);
        if (a is null || b is null)
            return NotFound(ApiResponse.Failure(new[] { "One or both BOMs not found." }, 404));

        var setA = a.Components.ToDictionary(c => c.ComponentItemId, c => c.QuantityPerParent);
        var setB = b.Components.ToDictionary(c => c.ComponentItemId, c => c.QuantityPerParent);
        var allItems = setA.Keys.Union(setB.Keys);

        var rows = new List<BomComparisonRowDto>();
        foreach (var item in allItems)
        {
            setA.TryGetValue(item, out var qa);
            setB.TryGetValue(item, out var qb);
            rows.Add(new BomComparisonRowDto
            {
                ComponentItemId = item,
                QuantityInA = qa,
                QuantityInB = qb,
                Difference = qb - qa,
                Status = qa == qb ? "Same" : "Changed",
            });
        }

        return Ok(ApiResponse<BomComparisonDto>.Success(new BomComparisonDto
        {
            BomAId = bomA,
            BomBId = bomB,
            Rows = rows,
        }));
    }

    // --- Mass BOM Update (global component replacement) ---
    [HttpPost("mass-update")]
    public async Task<ActionResult<ApiResponse<BomMassUpdateResultDto>>> MassUpdate(
        [FromBody] BomMassUpdateRequest request, CancellationToken cancellationToken)
    {
        var affected = await _context.BomComponentLines
            .Where(c => c.ComponentItemId == request.FromItemId)
            .ToListAsync(cancellationToken);

        var updatedIds = new List<Guid>();
        foreach (var line in affected)
        {
            line.ReplaceComponent(request.ToItemId);
            updatedIds.Add(line.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<BomMassUpdateResultDto>.Success(new BomMassUpdateResultDto
        {
            LinesUpdated = updatedIds.Count,
            LineIds = updatedIds,
        }));
    }

    // --- Mapping Helpers ---
    private static BomSubstitutionDto MapSubstitution(BomComponentSubstitution s) => new ()
    {
        Id = s.Id,
        BomHeaderId = s.BomHeaderId,
        ComponentLineId = s.ComponentLineId,
        SubstituteItemId = s.SubstituteItemId,
        Reason = s.Reason,
        CostVariance = s.CostVariance,
        Priority = s.Priority,
        IsApproved = s.IsApproved,
    };

    private static BomAllocationDto MapAllocation(ComponentAllocation a) => new ()
    {
        Id = a.Id,
        BomHeaderId = a.BomHeaderId,
        BuildOrderId = a.BuildOrderId,
        ComponentItemId = a.ComponentItemId,
        Quantity = a.Quantity,
        FulfilledQuantity = a.FulfilledQuantity,
        UnitOfMeasure = a.UnitOfMeasure,
        WarehouseId = a.WarehouseId,
        IsReleased = a.IsReleased,
    };

    private static BomEcnDto MapEcn(EngineeringChangeNotice e) => new ()
    {
        Id = e.Id,
        BomHeaderId = e.BomHeaderId,
        EcnNumber = e.EcnNumber,
        Title = e.Title,
        Description = e.Description,
        Status = e.Status.ToString(),
        PlannedEffectivity = e.PlannedEffectivity,
        ActualEffectivity = e.ActualEffectivity,
        Reviewer = e.Reviewer,
        Approver = e.Approver,
    };
    private static BomHeaderDto MapToDto(BomHeader h) => new ()
    {
        Id = h.Id,
        CompanyId = h.CompanyId,
        ParentItemId = h.ParentItemId,
        Revision = h.Revision,
        Description = h.Description,
        BomType = h.BomType.ToString(),
        Status = h.Status.ToString(),
        EffectiveFrom = h.EffectiveFrom,
        EffectiveTo = h.EffectiveTo,
        YieldPercentage = h.YieldPercentage,
        AlternateCode = h.AlternateCode,
        EstimatedMaterialCost = h.EstimatedMaterialCost,
        EstimatedLaborCost = h.EstimatedLaborCost,
        EstimatedOverheadCost = h.EstimatedOverheadCost,
        ComponentCount = h.Components.Count,
    };

    private static BomComponentLineDto MapComponentToDto(BomComponentLine c) => new ()
    {
        Id = c.Id,
        BomHeaderId = c.BomHeaderId,
        ComponentItemId = c.ComponentItemId,
        QuantityPerParent = c.QuantityPerParent,
        EffectiveQuantity = c.EffectiveQuantity,
        UnitOfMeasure = c.UnitOfMeasure,
        ScrapFactor = c.ScrapFactor,
        OperationSequence = c.OperationSequence,
        WorkCenterId = c.WorkCenterId,
        IsPhantom = c.IsPhantom,
        IsCritical = c.IsCritical,
        EstimatedUnitCost = c.EstimatedUnitCost,
        Notes = c.Notes,
    };
}

// --- DTOs ---
#pragma warning disable S6960

public class BomHeaderDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ParentItemId { get; set; }
    public string Revision { get; set; } = string.Empty;
    public string? AlternateCode { get; set; }
    public string? Description { get; set; }
    public string BomType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public decimal YieldPercentage { get; set; }
    public decimal? EstimatedMaterialCost { get; set; }
    public decimal? EstimatedLaborCost { get; set; }
    public decimal? EstimatedOverheadCost { get; set; }
    public int ComponentCount { get; set; }
}

public class BomComponentLineDto
{
    public Guid Id { get; set; }
    public Guid BomHeaderId { get; set; }
    public Guid ComponentItemId { get; set; }
    public decimal QuantityPerParent { get; set; }
    public decimal EffectiveQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal ScrapFactor { get; set; }
    public int OperationSequence { get; set; }
    public Guid? WorkCenterId { get; set; }
    public bool IsPhantom { get; set; }
    public bool IsCritical { get; set; }
    public decimal? EstimatedUnitCost { get; set; }
    public string? Notes { get; set; }
}

public class CreateBomHeaderRequest
{
    public Guid CompanyId { get; set; }
    public Guid ParentItemId { get; set; }
    public string Revision { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BomType { get; set; } = "Standard";
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public decimal? YieldPercentage { get; set; }
    public string? AlternateCode { get; set; }
}

public class UpdateBomHeaderRequest
{
    public string? Description { get; set; }
    public string? BomType { get; set; }
    public string? Status { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public decimal? YieldPercentage { get; set; }
    public string? AlternateCode { get; set; }
}

public class AddComponentRequest
{
    public Guid ComponentItemId { get; set; }
    public decimal QuantityPerParent { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public decimal? ScrapFactor { get; set; }
    public int? OperationSequence { get; set; }
    public Guid? WorkCenterId { get; set; }
    public bool IsPhantom { get; set; }
    public bool IsCritical { get; set; }
    public string? Notes { get; set; }
}

public class UpdateComponentRequest
{
    public decimal? QuantityPerParent { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? ScrapFactor { get; set; }
    public int? OperationSequence { get; set; }
    public Guid? WorkCenterId { get; set; }
    public bool? IsPhantom { get; set; }
    public bool? IsCritical { get; set; }
    public string? Notes { get; set; }
}

public class BomExplosionDto
{
    public int Level { get; set; }
    public Guid ComponentItemId { get; set; }
    public decimal QuantityPerParent { get; set; }
    public decimal NetQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal ScrapFactor { get; set; }
    public bool IsPhantom { get; set; }
    public bool IsCritical { get; set; }
    public int OperationSequence { get; set; }
}

public class BomWhereUsedDto
{
    public Guid BomHeaderId { get; set; }
    public Guid ParentItemId { get; set; }
    public string Revision { get; set; } = string.Empty;
    public decimal QuantityPerParent { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public bool IsPhantom { get; set; }
    public int OperationSequence { get; set; }
}

#pragma warning disable CA1002, CA2227
public class BomCostRollupDto
{
    public Guid BomHeaderId { get; set; }
    public Guid ParentItemId { get; set; }
    public string Revision { get; set; } = string.Empty;
    public decimal YieldPercentage { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalCost { get; set; }
    public List<BomCostRollupLineDto> Components { get; set; } = [];
}
#pragma warning restore CA1002, CA2227

public class BomCostRollupLineDto
{
    public Guid ComponentItemId { get; set; }
    public decimal QuantityPerParent { get; set; }
    public decimal EffectiveQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ExtendedCost { get; set; }
    public decimal ScrapFactor { get; set; }
}

// --- Substitution / Allocation / ECN / Comparison / Mass-Update DTOs ---
public class BomSubstitutionDto
{
    public Guid Id { get; set; }
    public Guid BomHeaderId { get; set; }
    public Guid ComponentLineId { get; set; }
    public Guid SubstituteItemId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? CostVariance { get; set; }
    public int Priority { get; set; }
    public bool IsApproved { get; set; }
}

public class AddSubstitutionRequest
{
    public Guid ComponentLineId { get; set; }
    public Guid SubstituteItemId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? CostVariance { get; set; }
    public int Priority { get; set; } = 10;
}

public class BomAllocationDto
{
    public Guid Id { get; set; }
    public Guid BomHeaderId { get; set; }
    public Guid BuildOrderId { get; set; }
    public Guid ComponentItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal FulfilledQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public bool IsReleased { get; set; }
}

public class AllocateComponentRequest
{
    public Guid BuildOrderId { get; set; }
    public Guid ComponentItemId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public Guid? WarehouseId { get; set; }
}

public class BomEcnDto
{
    public Guid Id { get; set; }
    public Guid BomHeaderId { get; set; }
    public string EcnNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PlannedEffectivity { get; set; }
    public DateTime? ActualEffectivity { get; set; }
    public string? Reviewer { get; set; }
    public string? Approver { get; set; }
}

public class CreateEcnRequest
{
    public Guid CompanyId { get; set; }
    public string EcnNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? PlannedEffectivity { get; set; }
}

public class EcnTransitionRequest
{
    public string? Action { get; set; }
    public string? Reviewer { get; set; }
    public string? Approver { get; set; }
    public string? Reason { get; set; }
    public DateTime? Effectivity { get; set; }
}

#pragma warning disable CA1002, CA2227
public class BomComparisonDto
{
    public Guid BomAId { get; set; }
    public Guid BomBId { get; set; }
    public List<BomComparisonRowDto> Rows { get; set; } = [];
}
#pragma warning restore CA1002, CA2227

public class BomComparisonRowDto
{
    public Guid ComponentItemId { get; set; }
    public decimal QuantityInA { get; set; }
    public decimal QuantityInB { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = string.Empty;
}

#pragma warning disable CA1002, CA2227
public class BomMassUpdateRequest
{
    public Guid FromItemId { get; set; }
    public Guid ToItemId { get; set; }
}

public class BomMassUpdateResultDto
{
    public int LinesUpdated { get; set; }
    public List<Guid> LineIds { get; set; } = [];
}
#pragma warning restore CA1002, CA2227

#pragma warning disable CA1002, CA2227
public class WhatIfRequest
{
    public List<WhatIfComponentOverride>? Overrides { get; set; }
}
#pragma warning restore CA1002, CA2227

public class WhatIfComponentOverride
{
    public Guid ComponentLineId { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? QuantityPerParent { get; set; }
}

public class WhatIfSimulationDto
{
    public Guid BomHeaderId { get; set; }
    public decimal CurrentMaterialCost { get; set; }
    public decimal SimulatedMaterialCost { get; set; }
    public decimal Delta { get; set; }
    public decimal? PercentChange { get; set; }
    public int AppliedOverrides { get; set; }
}

public class SuggestRequisitionsRequest
{
    public decimal PlannedQuantity { get; set; }
}
