// <copyright file="ShippingMethodController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing/shipping-methods")]
public class ShippingMethodController : ControllerBase
{
    private readonly IRepository<ShippingMethod> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ShippingMethodController(IRepository<ShippingMethod> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ShippingMethodDto>>>> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var methods = await _repository.GetAllAsync(cancellationToken);

        if (activeOnly == true)
            methods = methods.Where(m => m.IsActive).ToList();

        var dtos = methods.Select(m => new ShippingMethodDto
        {
            Id = m.Id,
            Code = m.Code,
            Description = m.Description,
            CarrierName = m.CarrierName,
            StandardLeadTimeDays = m.StandardLeadTimeDays,
            IsActive = m.IsActive,
        }).ToList();

        return Ok(ApiResponse<List<ShippingMethodDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ShippingMethodDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var method = await _repository.GetByIdAsync(id, cancellationToken);

        if (method == null)
            return NotFound(ApiResponse<ShippingMethodDto>.Failure(["Shipping method not found."]));

        var dto = new ShippingMethodDto
        {
            Id = method.Id,
            Code = method.Code,
            Description = method.Description,
            CarrierName = method.CarrierName,
            StandardLeadTimeDays = method.StandardLeadTimeDays,
            IsActive = method.IsActive,
        };

        return Ok(ApiResponse<ShippingMethodDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ShippingMethodDto>>> Create(
        [FromBody] CreateShippingMethodRequest request,
        CancellationToken cancellationToken)
    {
        var method = new ShippingMethod(
            request.Code,
            request.Description,
            request.CarrierName,
            request.CarrierAccountNumber,
            request.StandardLeadTimeDays,
            request.IsActive);

        await _repository.AddAsync(method, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ShippingMethodDto
        {
            Id = method.Id,
            Code = method.Code,
            Description = method.Description,
            CarrierName = method.CarrierName,
            StandardLeadTimeDays = method.StandardLeadTimeDays,
            IsActive = method.IsActive,
        };

        return CreatedAtAction(nameof(GetById), new { id = method.Id }, ApiResponse<ShippingMethodDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ShippingMethodDto>>> Update(
        Guid id,
        [FromBody] UpdateShippingMethodRequest request,
        CancellationToken cancellationToken)
    {
        var method = await _repository.GetByIdAsync(id, cancellationToken);

        if (method == null)
            return NotFound(ApiResponse<ShippingMethodDto>.Failure(["Shipping method not found."]));

        method.UpdateDescription(request.Description);
        method.UpdateCarrierInfo(request.CarrierName, request.CarrierAccountNumber);
        method.UpdateLeadTime(request.StandardLeadTimeDays);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                method.Activate();
            else
                method.Deactivate();
        }

        _repository.Update(method);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ShippingMethodDto
        {
            Id = method.Id,
            Code = method.Code,
            Description = method.Description,
            CarrierName = method.CarrierName,
            StandardLeadTimeDays = method.StandardLeadTimeDays,
            IsActive = method.IsActive,
        };

        return Ok(ApiResponse<ShippingMethodDto>.Success(dto));
    }
}

public class ShippingMethodDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CarrierName { get; set; }
    public decimal StandardLeadTimeDays { get; set; }
    public bool IsActive { get; set; }
}

public class CreateShippingMethodRequest
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CarrierName { get; set; }
    public string? CarrierAccountNumber { get; set; }
    public decimal StandardLeadTimeDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateShippingMethodRequest
{
    public string Description { get; set; } = string.Empty;
    public string? CarrierName { get; set; }
    public string? CarrierAccountNumber { get; set; }
    public decimal StandardLeadTimeDays { get; set; }
    public bool? IsActive { get; set; }
}
