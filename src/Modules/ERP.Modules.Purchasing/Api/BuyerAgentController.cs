// <copyright file="BuyerAgentController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing/buyer-agents")]
public class BuyerAgentController : ControllerBase
{
    private readonly IRepository<BuyerAgent> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public BuyerAgentController(IRepository<BuyerAgent> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BuyerAgentDto>>>> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var buyers = await _repository.GetAllAsync(cancellationToken);

        if (activeOnly == true)
            buyers = buyers.Where(b => b.IsActive).ToList();

        var dtos = buyers.Select(b => new BuyerAgentDto
        {
            Id = b.Id,
            BuyerCode = b.BuyerCode,
            Name = b.Name,
            Email = b.Email,
            Phone = b.Phone,
            ApprovalLimit = b.ApprovalLimit,
            IsActive = b.IsActive,
        }).ToList();

        return Ok(ApiResponse<List<BuyerAgentDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BuyerAgentDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var buyer = await _repository.GetByIdAsync(id, cancellationToken);

        if (buyer == null)
            return NotFound(ApiResponse<BuyerAgentDto>.Failure(["Buyer agent not found."]));

        var dto = new BuyerAgentDto
        {
            Id = buyer.Id,
            BuyerCode = buyer.BuyerCode,
            Name = buyer.Name,
            Email = buyer.Email,
            Phone = buyer.Phone,
            ApprovalLimit = buyer.ApprovalLimit,
            IsActive = buyer.IsActive,
        };

        return Ok(ApiResponse<BuyerAgentDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BuyerAgentDto>>> Create(
        [FromBody] CreateBuyerAgentRequest request,
        CancellationToken cancellationToken)
    {
        var buyer = new BuyerAgent(
            request.BuyerCode,
            request.Name,
            request.UserId,
            request.Email,
            request.Phone,
            request.ApprovalLimit,
            request.IsActive);

        await _repository.AddAsync(buyer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new BuyerAgentDto
        {
            Id = buyer.Id,
            BuyerCode = buyer.BuyerCode,
            Name = buyer.Name,
            Email = buyer.Email,
            Phone = buyer.Phone,
            ApprovalLimit = buyer.ApprovalLimit,
            IsActive = buyer.IsActive,
        };

        return CreatedAtAction(nameof(GetById), new { id = buyer.Id }, ApiResponse<BuyerAgentDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BuyerAgentDto>>> Update(
        Guid id,
        [FromBody] UpdateBuyerAgentRequest request,
        CancellationToken cancellationToken)
    {
        var buyer = await _repository.GetByIdAsync(id, cancellationToken);

        if (buyer == null)
            return NotFound(ApiResponse<BuyerAgentDto>.Failure(["Buyer agent not found."]));

        buyer.UpdateApprovalLimit(request.ApprovalLimit);
        buyer.UpdateContactInfo(request.Email, request.Phone);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                buyer.Activate();
            else
                buyer.Deactivate();
        }

        _repository.Update(buyer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new BuyerAgentDto
        {
            Id = buyer.Id,
            BuyerCode = buyer.BuyerCode,
            Name = buyer.Name,
            Email = buyer.Email,
            Phone = buyer.Phone,
            ApprovalLimit = buyer.ApprovalLimit,
            IsActive = buyer.IsActive,
        };

        return Ok(ApiResponse<BuyerAgentDto>.Success(dto));
    }
}

public class BuyerAgentDto
{
    public Guid Id { get; set; }
    public string BuyerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal ApprovalLimit { get; set; }
    public bool IsActive { get; set; }
}

public class CreateBuyerAgentRequest
{
    public string BuyerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal ApprovalLimit { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateBuyerAgentRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal ApprovalLimit { get; set; }
    public bool? IsActive { get; set; }
}
