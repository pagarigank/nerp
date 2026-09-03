// <copyright file="EInvoiceController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class EInvoiceController : ControllerBase
{
    private readonly IEInvoiceService _service;

    public EInvoiceController(IEInvoiceService service)
    {
        _service = service;
    }

    [HttpPost("documents")]
    [ProducesResponseType(typeof(EInvoiceSubmissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitInvoice([FromBody] EInvoiceSubmitRequest request)
    {
        var result = await _service.SubmitDocumentAsync(
            request.CompanyId,
            request.SourceDocumentId,
            request.DocumentType,
            request.Payload);

        if (!result.Success)
            return BadRequest(new { Error = result.Error });

        return Ok(result);
    }

    [HttpGet("documents/{documentId:guid}/status")]
    [ProducesResponseType(typeof(EInvoiceDocumentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentStatus(Guid documentId)
    {
        try
        {
            var status = await _service.GetDocumentStatusAsync(documentId);
            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    [HttpGet("tin-validation/{tin}")]
    [ProducesResponseType(typeof(EInvoiceValidationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateTin(string tin)
    {
        var result = await _service.ValidateTinAsync(tin);
        return Ok(result);
    }

    [HttpPost("documents/{documentId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelDocument(Guid documentId, [FromBody] CancelRequest request)
    {
        try
        {
            var success = await _service.CancelDocumentAsync(documentId, request.Reason);
            if (success)
                return Ok(new { Message = "Document cancelled successfully" });
            return BadRequest(new { Error = "Document could not be cancelled" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

public class EInvoiceSubmitRequest
{
    [Required] public Guid CompanyId { get; set; }
    [Required] public Guid SourceDocumentId { get; set; }
    [Required] public string DocumentType { get; set; } = "Invoice";
    [Required] public string Payload { get; set; } = string.Empty;
}

public class CancelRequest
{
    [Required] public string Reason { get; set; } = string.Empty;
}