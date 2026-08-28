// <copyright file="ProjectDocumentsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable CA3003
#pragma warning disable SA1501

using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/projects/{projectId:guid}/documents")]
public class ProjectDocumentsController : ControllerBase
{
    private readonly ProjDbContext _context;
    private readonly IProjUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ProjectDocumentsController(ProjDbContext context, IProjUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    private static ProjectDocumentDto MapToDto(ProjectDocument d) => new ()
    {
        Id = d.Id,
        CompanyId = d.CompanyId,
        ProjectId = d.ProjectId,
        Name = d.Name,
        DocumentType = d.DocumentType,
        FileReference = d.FileReference,
        ContentType = d.ContentType,
        SizeBytes = d.SizeBytes,
        UploadedBy = d.UploadedBy,
        UploadedOn = d.UploadedOn,
    };

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProjectDocumentDto>>>> GetDocuments(
        Guid projectId, CancellationToken cancellationToken)
    {
        var documents = await _context.ProjectDocuments
            .ApplyCompanyScope(HttpContext, d => d.CompanyId)
            .Where(d => d.ProjectId == projectId && d.DeletedOn == null)
            .OrderByDescending(d => d.UploadedOn)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<ProjectDocumentDto>>.Success(documents.Select(MapToDto).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateDocument(
        Guid projectId,
        [FromBody] CreateProjectDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .ApplyCompanyScope(HttpContext, p => p.CompanyId)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse.Failure(new[] { "Name is required." }));
        if (string.IsNullOrWhiteSpace(request.DocumentType))
            return BadRequest(ApiResponse.Failure(new[] { "Document type is required." }));
        if (string.IsNullOrWhiteSpace(request.FileReference))
            return BadRequest(ApiResponse.Failure(new[] { "File reference is required." }));

        var document = new ProjectDocument(
            project.CompanyId,
            project.Id,
            request.Name,
            request.DocumentType,
            request.FileReference,
            request.ContentType,
            request.SizeBytes,
            _currentUser.UserId ?? "system");

        _context.ProjectDocuments.Add(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(document.Id));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<ApiResponse<Guid>>> UploadDocument(
        Guid projectId,
        IFormFile file,
        [FromQuery] string documentType = "General",
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects.ApplyCompanyScope(HttpContext, p => p.CompanyId).FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null) return NotFound(ApiResponse.Failure(new[] { "Project not found." }, 404));
        if (file is null || file.Length == 0) return BadRequest(ApiResponse.Failure(new[] { "File is required." }));
        var uploadsRoot = Path.Combine(Path.GetTempPath(), "erp-project-docs", projectId.ToString());
        Directory.CreateDirectory(uploadsRoot);
        var safeName = Path.GetFileName(file.FileName);
        var storedName = $"{Guid.NewGuid()}_{safeName}";
        var fullPath = Path.Combine(uploadsRoot, storedName);
        await using (var stream = new FileStream(fullPath, FileMode.Create)) await file.CopyToAsync(stream, cancellationToken);
        var document = new ProjectDocument(project.CompanyId, project.Id, safeName, documentType, fullPath, file.ContentType, file.Length, _currentUser.UserId ?? "system");
        _context.ProjectDocuments.Add(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(document.Id));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid projectId, Guid id, CancellationToken cancellationToken)
    {
        var doc = await _context.ProjectDocuments.ApplyCompanyScope(HttpContext, d => d.CompanyId).FirstOrDefaultAsync(d => d.Id == id && d.ProjectId == projectId && d.DeletedOn == null, cancellationToken);
        if (doc is null) return NotFound();
        if (!System.IO.File.Exists(doc.FileReference)) return NotFound();
        var bytes = await System.IO.File.ReadAllBytesAsync(doc.FileReference, cancellationToken);
        return File(bytes, doc.ContentType ?? "application/octet-stream", doc.Name);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteDocument(
        Guid projectId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await _context.ProjectDocuments
            .ApplyCompanyScope(HttpContext, d => d.CompanyId)
            .FirstOrDefaultAsync(d => d.Id == id && d.ProjectId == projectId && d.DeletedOn == null, cancellationToken);
        if (document is null)
            return NotFound(ApiResponse.Failure(new[] { "Document not found." }, 404));

        document.MarkDeleted(_currentUser.UserId ?? "system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }
}

public class ProjectDocumentDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string FileReference { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTimeOffset UploadedOn { get; set; }
}

public class CreateProjectDocumentRequest
{
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string FileReference { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
}
