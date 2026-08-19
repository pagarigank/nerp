// <copyright file="RequisitionConversionController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing/requisition-conversion")]
public class RequisitionConversionController : ControllerBase
{
    private readonly IRequisitionToPOService _conversionService;

    public RequisitionConversionController(IRequisitionToPOService conversionService)
    {
        _conversionService = conversionService;
    }

    [HttpPost("convert-single")]
    public async Task<ActionResult<ApiResponse<ConversionResultDto>>> ConvertSingle(
        [FromBody] ConvertSingleRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var poId = await _conversionService.ConvertRequisitionToPOAsync(
                request.RequisitionId,
                request.PreferredVendorId,
                cancellationToken);

            var result = new ConversionResultDto
            {
                PurchaseOrderIds = [poId],
                Message = "Requisition successfully converted to Purchase Order.",
            };

            return Ok(ApiResponse<ConversionResultDto>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ConversionResultDto>.Failure([ex.Message], 400));
        }
    }

    [HttpPost("consolidate")]
    public async Task<ActionResult<ApiResponse<ConversionResultDto>>> Consolidate(
        [FromBody] ConsolidateRequisitionsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var poIds = await _conversionService.ConsolidateRequisitionsToPOAsync(
                request.RequisitionIds,
                request.VendorId,
                cancellationToken);

            var result = new ConversionResultDto
            {
                PurchaseOrderIds = poIds,
                Message = $"Successfully consolidated {request.RequisitionIds.Count} requisitions into {poIds.Count} Purchase Order(s).",
            };

            return Ok(ApiResponse<ConversionResultDto>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ConversionResultDto>.Failure([ex.Message], 400));
        }
    }
}

public class ConvertSingleRequisitionRequest
{
    public Guid RequisitionId { get; set; }
    public Guid? PreferredVendorId { get; set; }
}

public class ConsolidateRequisitionsRequest
{
    public List<Guid> RequisitionIds { get; set; } = [];
    public Guid VendorId { get; set; }
}

public class ConversionResultDto
{
    public List<Guid> PurchaseOrderIds { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}
