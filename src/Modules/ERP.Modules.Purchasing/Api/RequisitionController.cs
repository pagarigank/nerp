// <copyright file="RequisitionController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing/requisitions")]
public class RequisitionController : ControllerBase
{
    private readonly IRepository<Requisition> _requisitionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RequisitionController(
        IRepository<Requisition> requisitionRepository,
        IUnitOfWork unitOfWork)
    {
        _requisitionRepository = requisitionRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RequisitionDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var requisitions = await _requisitionRepository.GetAllAsync(cancellationToken);
        var dtos = requisitions.Select(r => new RequisitionDto
        {
            Id = r.Id,
            RequisitionNumber = r.RequisitionNumber,
            CompanyId = r.CompanyId,
            RequestorId = r.RequestorId,
            RequestDate = r.RequestDate,
            NeedByDate = r.NeedByDate,
            Description = r.Description,
            Status = r.Status.ToString(),
            TotalAmount = r.GetTotalAmount(),
        }).ToList();

        return Ok(ApiResponse<List<RequisitionDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RequisitionDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var requisition = await _requisitionRepository.GetByIdAsync(id, cancellationToken);

        if (requisition == null)
        {
            return NotFound(ApiResponse<RequisitionDto>.Failure(["Requisition not found."]));
        }

        var dto = new RequisitionDto
        {
            Id = requisition.Id,
            RequisitionNumber = requisition.RequisitionNumber,
            CompanyId = requisition.CompanyId,
            RequestorId = requisition.RequestorId,
            RequestDate = requisition.RequestDate,
            NeedByDate = requisition.NeedByDate,
            Description = requisition.Description,
            Status = requisition.Status.ToString(),
            TotalAmount = requisition.GetTotalAmount(),
        };

        return Ok(ApiResponse<RequisitionDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RequisitionDto>>> Create(
        [FromBody] CreateRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = new Requisition(
            request.RequisitionNumber,
            request.CompanyId,
            request.RequestorId,
            request.RequestDate,
            request.NeedByDate,
            request.Description);

        foreach (var lineRequest in request.Lines)
        {
            var line = new RequisitionLine(
                requisition.Id,
                lineRequest.LineNumber,
                lineRequest.ItemId,
                lineRequest.Description,
                lineRequest.Quantity,
                lineRequest.UnitOfMeasure,
                lineRequest.EstimatedUnitPrice,
                lineRequest.NeedByDate,
                lineRequest.PreferredVendorId,
                lineRequest.AccountId,
                lineRequest.ProjectId,
                lineRequest.TaskId);

            requisition.AddLine(line);
        }

        await _requisitionRepository.AddAsync(requisition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new RequisitionDto
        {
            Id = requisition.Id,
            RequisitionNumber = requisition.RequisitionNumber,
            CompanyId = requisition.CompanyId,
            RequestorId = requisition.RequestorId,
            RequestDate = requisition.RequestDate,
            NeedByDate = requisition.NeedByDate,
            Description = requisition.Description,
            Status = requisition.Status.ToString(),
            TotalAmount = requisition.GetTotalAmount(),
        };

        return CreatedAtAction(nameof(GetById), new { id = requisition.Id }, ApiResponse<RequisitionDto>.Success(dto));
    }

    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<ActionResult<ApiResponse<RequisitionDto>>> SubmitForApproval(
        Guid id,
        CancellationToken cancellationToken)
    {
        var requisition = await _requisitionRepository.GetByIdAsync(id, cancellationToken);

        if (requisition == null)
        {
            return NotFound(ApiResponse<RequisitionDto>.Failure(["Requisition not found."]));
        }

        requisition.SubmitForApproval();
        _requisitionRepository.Update(requisition);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new RequisitionDto
        {
            Id = requisition.Id,
            RequisitionNumber = requisition.RequisitionNumber,
            CompanyId = requisition.CompanyId,
            RequestorId = requisition.RequestorId,
            RequestDate = requisition.RequestDate,
            NeedByDate = requisition.NeedByDate,
            Description = requisition.Description,
            Status = requisition.Status.ToString(),
            TotalAmount = requisition.GetTotalAmount(),
        };

        return Ok(ApiResponse<RequisitionDto>.Success(dto));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<RequisitionDto>>> Approve(
        Guid id,
        [FromBody] ApproveRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await _requisitionRepository.GetByIdAsync(id, cancellationToken);

        if (requisition == null)
        {
            return NotFound(ApiResponse<RequisitionDto>.Failure(["Requisition not found."]));
        }

        requisition.Approve(request.ApprovedById);
        _requisitionRepository.Update(requisition);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new RequisitionDto
        {
            Id = requisition.Id,
            RequisitionNumber = requisition.RequisitionNumber,
            CompanyId = requisition.CompanyId,
            RequestorId = requisition.RequestorId,
            RequestDate = requisition.RequestDate,
            NeedByDate = requisition.NeedByDate,
            Description = requisition.Description,
            Status = requisition.Status.ToString(),
            TotalAmount = requisition.GetTotalAmount(),
        };

        return Ok(ApiResponse<RequisitionDto>.Success(dto));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<RequisitionDto>>> Reject(
        Guid id,
        [FromBody] RejectRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await _requisitionRepository.GetByIdAsync(id, cancellationToken);

        if (requisition == null)
        {
            return NotFound(ApiResponse<RequisitionDto>.Failure(["Requisition not found."]));
        }

        requisition.Reject(request.RejectedById, request.Reason);
        _requisitionRepository.Update(requisition);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new RequisitionDto
        {
            Id = requisition.Id,
            RequisitionNumber = requisition.RequisitionNumber,
            CompanyId = requisition.CompanyId,
            RequestorId = requisition.RequestorId,
            RequestDate = requisition.RequestDate,
            NeedByDate = requisition.NeedByDate,
            Description = requisition.Description,
            Status = requisition.Status.ToString(),
            TotalAmount = requisition.GetTotalAmount(),
        };

        return Ok(ApiResponse<RequisitionDto>.Success(dto));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<RequisitionDto>>> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var requisition = await _requisitionRepository.GetByIdAsync(id, cancellationToken);

        if (requisition == null)
        {
            return NotFound(ApiResponse<RequisitionDto>.Failure(["Requisition not found."]));
        }

        requisition.Cancel();
        _requisitionRepository.Update(requisition);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new RequisitionDto
        {
            Id = requisition.Id,
            RequisitionNumber = requisition.RequisitionNumber,
            CompanyId = requisition.CompanyId,
            RequestorId = requisition.RequestorId,
            RequestDate = requisition.RequestDate,
            NeedByDate = requisition.NeedByDate,
            Description = requisition.Description,
            Status = requisition.Status.ToString(),
            TotalAmount = requisition.GetTotalAmount(),
        };

        return Ok(ApiResponse<RequisitionDto>.Success(dto));
    }
}

public class RequisitionDto
{
    public Guid Id { get; set; }

    public string RequisitionNumber { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public Guid RequestorId { get; set; }

    public DateTime RequestDate { get; set; }

    public DateTime? NeedByDate { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
}

public class CreateRequisitionRequest
{
    public string RequisitionNumber { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public Guid RequestorId { get; set; }

    public DateTime RequestDate { get; set; }

    public DateTime? NeedByDate { get; set; }

    public string? Description { get; set; }

    public List<CreateRequisitionLineRequest> Lines { get; set; } = [];
}

public class CreateRequisitionLineRequest
{
    public int LineNumber { get; set; }

    public string? ItemId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string UnitOfMeasure { get; set; } = string.Empty;

    public decimal EstimatedUnitPrice { get; set; }

    public DateTime? NeedByDate { get; set; }

    public Guid? PreferredVendorId { get; set; }

    public Guid? AccountId { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? TaskId { get; set; }
}

public class ApproveRequisitionRequest
{
    public Guid ApprovedById { get; set; }
}

public class RejectRequisitionRequest
{
    public Guid RejectedById { get; set; }
    public string Reason { get; set; } = string.Empty;
}
