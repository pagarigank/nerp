// <copyright file="ImportJobController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using Asp.Versioning;
using ERP.Modules.Integration.Domain.Entities;
using ERP.Modules.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Integration.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration/import-jobs")]
public class ImportJobController : ControllerBase
{
    private readonly IntegrationDbContext _context;

    public ImportJobController(IntegrationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImportJobDto>>> GetAll(
        [FromQuery] Guid? companyId, [FromQuery] string? status, CancellationToken ct)
    {
        var query = _context.ImportJobs
            .AsNoTracking()
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        var jobs = await query.OrderByDescending(x => x.CreatedOn).ToListAsync(ct);
        return Ok(jobs.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ImportJobDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var job = await _context.ImportJobs
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return NotFound();
        return Ok(MapToDetailDto(job));
    }

    [HttpPost]
    public async Task<ActionResult<ImportJobDto>> Create(
        [FromBody] CreateImportJobRequest request, CancellationToken ct)
    {
        var job = new ImportJob(
            request.CompanyId, request.JobName, request.FileFormat,
            request.Destination, request.FileName, request.EdiDocumentMappingId);
        _context.ImportJobs.Add(job);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, MapToDto(job));
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<IActionResult> AddLines(
        Guid id, [FromBody] AddImportLinesRequest request, CancellationToken ct)
    {
        var job = await _context.ImportJobs.FindAsync(new object[] { id }, ct);
        if (job is null) return NotFound();
        if (job.Status != "Draft") return BadRequest("Lines can only be added to a Draft job.");

        var startRow = job.TotalRows;
        foreach (var row in request.Rows)
        {
            startRow++;
            var line = new ImportJobLine(id, startRow, row);
            job.AddLine(line);
            _context.ImportJobLines.Add(line);
        }

        await _context.SaveChangesAsync(ct);
        return Ok(new { added = request.Rows.Count, totalRows = job.TotalRows });
    }

    [HttpPost("{id:guid}/validate")]
    public async Task<ActionResult<ImportValidationResult>> Validate(Guid id, CancellationToken ct)
    {
        var job = await _context.ImportJobs
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return NotFound();
        if (job.Status != "Draft" && job.Status != "Failed")
            return BadRequest("Job must be in Draft or Failed status to validate.");

        job.MarkValidating();
        var validCount = 0;
        var errorCount = 0;

        foreach (var line in job.Lines)
        {
            // Basic JSON validation — each row must be parseable
            try
            {
                if (string.IsNullOrWhiteSpace(line.RowData))
                    throw new InvalidOperationException("Row data is empty.");

                JsonDocument.Parse(line.RowData);
                line.MarkValid();
                validCount++;
            }
            catch (Exception ex)
            {
                line.MarkInvalid(ex.Message);
                errorCount++;
            }
        }

        job.MarkValidated(validCount, errorCount);
        await _context.SaveChangesAsync(ct);

        return Ok(new ImportValidationResult(
            job.Id, job.TotalRows, validCount, errorCount, job.Status));
    }

    [HttpPost("{id:guid}/commit")]
    public async Task<IActionResult> Commit(Guid id, CancellationToken ct)
    {
        var job = await _context.ImportJobs
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return NotFound();
        if (job.Status != "Validated")
            return BadRequest("Job must be in Validated status to commit.");
        if (job.ErrorRows > 0)
            return BadRequest($"Cannot commit: {job.ErrorRows} rows have validation errors. Fix and re-validate.");

        job.MarkCommitting();

        // Simulate commit — mark all valid lines as imported
        var committed = 0;
        foreach (var line in job.Lines.Where(l => l.ValidationStatus == "Valid"))
        {
            line.MarkImported();
            committed++;
        }

        job.MarkCommitted(committed);
        await _context.SaveChangesAsync(ct);
        return Ok(new { jobId = job.Id, committedRows = committed, status = job.Status });
    }

    [HttpPost("{id:guid}/reset")]
    public async Task<IActionResult> Reset(Guid id, CancellationToken ct)
    {
        var job = await _context.ImportJobs.FindAsync(new object[] { id }, ct);
        if (job is null) return NotFound();
        if (job.Status == "Committed")
            return BadRequest("Cannot reset a committed job.");
        job.MarkDraft();
        await _context.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var job = await _context.ImportJobs
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return NotFound();
        if (job.Status == "Committed")
            return BadRequest("Cannot delete a committed job.");
        _context.ImportJobLines.RemoveRange(job.Lines);
        _context.ImportJobs.Remove(job);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ImportJobDto MapToDto(ImportJob j) => new(
        j.Id, j.CompanyId, j.JobName, j.FileFormat, j.Destination,
        j.FileName, j.Status, j.TotalRows, j.ValidatedRows,
        j.ErrorRows, j.CommittedRows, j.ErrorMessage, j.CreatedOn);

    private static ImportJobDetailDto MapToDetailDto(ImportJob j) => new(
        j.Id, j.CompanyId, j.JobName, j.FileFormat, j.Destination,
        j.FileName, j.EdiDocumentMappingId, j.Status, j.TotalRows,
        j.ValidatedRows, j.ErrorRows, j.CommittedRows,
        j.ErrorMessage, j.ValidatedOn, j.CommittedOn,
        j.Lines.Select(l => new ImportLineDto(
            l.Id, l.RowNumber, l.RowData, l.ValidationStatus, l.Errors, l.Imported)).ToList());

    public record ImportJobDto(
        Guid Id, Guid CompanyId, string JobName, string FileFormat,
        string Destination, string? FileName, string Status,
        int TotalRows, int ValidatedRows, int ErrorRows, int CommittedRows,
        string? ErrorMessage, DateTimeOffset? CreatedOn);

    public record ImportJobDetailDto(
        Guid Id, Guid CompanyId, string JobName, string FileFormat,
        string Destination, string? FileName, Guid? EdiDocumentMappingId,
        string Status, int TotalRows, int ValidatedRows, int ErrorRows,
        int CommittedRows, string? ErrorMessage, DateTimeOffset? ValidatedOn,
        DateTimeOffset? CommittedOn, IReadOnlyList<ImportLineDto> Lines);

    public record ImportLineDto(
        Guid Id, int RowNumber, string RowData, string ValidationStatus,
        string? Errors, bool Imported);

    public record CreateImportJobRequest(
        Guid CompanyId, string JobName, string FileFormat,
        string Destination, string? FileName, Guid? EdiDocumentMappingId);

    public record AddImportLinesRequest(IReadOnlyList<string> Rows);

    public record ImportValidationResult(
        Guid JobId, int TotalRows, int ValidRows, int ErrorRows, string Status);
}
