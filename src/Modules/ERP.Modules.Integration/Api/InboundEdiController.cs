// <copyright file="InboundEdiController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Integration.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Integration.Api;

/// <summary>
/// Receives and processes inbound EDI documents (850 Purchase Orders,
/// 810 Invoices, 856 ASNs). Parses the raw EDI content, creates the
/// corresponding ERP entities, and logs the transaction.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration/edi/inbound")]
public class InboundEdiController : ControllerBase
{
    private readonly InboundEdiProcessor _processor;

    public InboundEdiController(InboundEdiProcessor processor) => _processor = processor;

    /// <summary>
    /// Process an inbound EDI document.
    /// </summary>
    /// <param name="request">EDI content and metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Processing result with entity reference.</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<InboundEdiResponseDto>>> ProcessInboundEdi(
        [FromBody] ProcessInboundEdiRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.EdiContent))
        {
            return BadRequest(ApiResponse<InboundEdiResponseDto>.Failure(new[] { "EDI content is required" }));
        }

        if (string.IsNullOrWhiteSpace(request.DocumentType))
        {
            return BadRequest(ApiResponse<InboundEdiResponseDto>.Failure(new[] { "Document type is required (850, 810, 856)" }));
        }

        if (!new[] { "850", "810", "856" }.Contains(request.DocumentType))
        {
            return BadRequest(ApiResponse<InboundEdiResponseDto>.Failure(new[] { $"Unsupported document type: {request.DocumentType}. Supported: 850, 810, 856" }));
        }

        var result = await _processor.ProcessAsync(
            companyId: request.CompanyId,
            ediContent: request.EdiContent,
            documentType: request.DocumentType,
            partnerCode: request.PartnerCode,
            ct: ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<InboundEdiResponseDto>.Failure(new[] { result.ErrorMessage ?? "Processing failed" }));

        return Ok(ApiResponse<InboundEdiResponseDto>.Success(new InboundEdiResponseDto
        {
            EntityId = result.EntityId,
            EntityType = result.EntityType,
            DocumentType = request.DocumentType,
            PartnerCode = request.PartnerCode,
        }));
    }

    /// <summary>
    /// Process multiple inbound EDI documents in batch.
    /// </summary>
    /// <param name="request">Batch of EDI documents.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Batch processing results.</returns>
    [HttpPost("batch")]
    public async Task<ActionResult<ApiResponse<BatchInboundEdiResponseDto>>> ProcessBatchInboundEdi(
        [FromBody] BatchProcessInboundEdiRequest request,
        CancellationToken ct)
    {
        if (request.Documents == null || request.Documents.Count == 0)
            return BadRequest(ApiResponse<BatchInboundEdiResponseDto>.Failure(new[] { "No documents provided" }));

        if (request.Documents.Count > 100)
            return BadRequest(ApiResponse<BatchInboundEdiResponseDto>.Failure(new[] { "Maximum 100 documents per batch" }));

        var results = new List<InboundEdiResponseDto>();
        int successCount = 0;
        int errorCount = 0;

        foreach (var doc in request.Documents)
        {
            var result = await _processor.ProcessAsync(
                companyId: request.CompanyId,
                ediContent: doc.EdiContent,
                documentType: doc.DocumentType,
                partnerCode: doc.PartnerCode,
                ct: ct);

            if (result.IsSuccess)
            {
                successCount++;
                results.Add(new InboundEdiResponseDto
                {
                    EntityId = result.EntityId,
                    EntityType = result.EntityType,
                    DocumentType = doc.DocumentType,
                    PartnerCode = doc.PartnerCode,
                });
            }
            else
            {
                errorCount++;
                results.Add(new InboundEdiResponseDto
                {
                    DocumentType = doc.DocumentType,
                    PartnerCode = doc.PartnerCode,
                    Error = result.ErrorMessage,
                });
            }
        }

        return Ok(ApiResponse<BatchInboundEdiResponseDto>.Success(new BatchInboundEdiResponseDto
        {
            TotalDocuments = request.Documents.Count,
            SuccessCount = successCount,
            ErrorCount = errorCount,
            Results = results,
        }));
    }
}

public class ProcessInboundEdiRequest
{
    public Guid CompanyId { get; set; }
    public string EdiContent { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? PartnerCode { get; set; }
}

public class BatchProcessInboundEdiRequest
{
    public Guid CompanyId { get; set; }
    public List<ProcessInboundEdiRequest> Documents { get; set; } = new();
}

public class InboundEdiResponseDto
{
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? PartnerCode { get; set; }
    public string? Error { get; set; }
}

public class BatchInboundEdiResponseDto
{
    public int TotalDocuments { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<InboundEdiResponseDto> Results { get; set; } = new();
}
