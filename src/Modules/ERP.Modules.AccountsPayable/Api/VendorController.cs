// <copyright file="VendorController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Linq.Expressions;
using Asp.Versioning;
using ERP.Modules.AccountsPayable.Api;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.AccountsPayable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ap/vendors")]
public class VendorController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public VendorController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VendorDto>>> GetAll([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        Expression<Func<Vendor, bool>> predicate = activeOnly.GetValueOrDefault()
            ? x => x.IsActive
            : x => true;

        var vendors = await _unitOfWork.Vendors.FindAsync(predicate, cancellationToken);
        return Ok(vendors.Select(v => MapToDto(v)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VendorDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.Vendors.GetByIdAsync(id, cancellationToken);
        if (vendor == null)
            return NotFound();

        var bankAccounts = await _unitOfWork.VendorBankAccounts.FindAsync(x => x.VendorId == id, cancellationToken);
        return Ok(MapToDto(vendor, bankAccounts.ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<VendorDto>> Create([FromBody] CreateVendorRequest request, CancellationToken cancellationToken)
    {
        var vendor = new Vendor(
            request.VendorId,
            request.Name,
            request.LegalName,
            request.TaxId,
            request.Form1099Category,
            request.DefaultPaymentTermId,
            request.IsActive,
            request.BackupWithholdingFlag,
            request.BackupWithholdingRate);

        if (request.BankAccounts?.Count > 0)
        {
            foreach (var ba in request.BankAccounts)
            {
                vendor.AddBankAccount(ba.BankName, ba.AccountNumber, ba.RoutingNumber, ba.IsDefault);
            }
        }

        vendor.SetCompliance(request.InsuranceCarrier, request.InsurancePolicyNumber, request.InsuranceExpiry, request.DiversityClassification);

        await _unitOfWork.Vendors.AddAsync(vendor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = vendor.Id }, MapToDto(vendor, []));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VendorDto>> Update(Guid id, [FromBody] UpdateVendorRequest request, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.Vendors.GetByIdAsync(id, cancellationToken);
        if (vendor == null)
            return NotFound();

        vendor.Update(request.Name, request.LegalName, request.TaxId, request.Form1099Category, request.DefaultPaymentTermId, request.BackupWithholdingFlag, request.BackupWithholdingRate);
        vendor.SetCompliance(request.InsuranceCarrier, request.InsurancePolicyNumber, request.InsuranceExpiry, request.DiversityClassification);
        _unitOfWork.Vendors.Update(vendor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(vendor, []));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.Vendors.GetByIdAsync(id, cancellationToken);
        if (vendor == null)
            return NotFound();

        vendor.Activate();
        _unitOfWork.Vendors.Update(vendor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.Vendors.GetByIdAsync(id, cancellationToken);
        if (vendor == null)
            return NotFound();

        vendor.Deactivate();
        _unitOfWork.Vendors.Update(vendor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/hold")]
    public async Task<IActionResult> SetHold(Guid id, [FromBody] SetVendorHoldRequest request, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.Vendors.GetByIdAsync(id, cancellationToken);
        if (vendor == null)
            return NotFound();

        vendor.SetOnHold(request.OnHold);
        _unitOfWork.Vendors.Update(vendor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static VendorDto MapToDto(Vendor vendor, List<VendorBankAccount>? bankAccounts = null)
    {
        bankAccounts ??= [];
        var baDtos = bankAccounts.Select(b => new VendorBankAccountDto(b.Id, b.BankName, b.AccountNumber, b.RoutingNumber, b.IsDefault)).ToList();
        return new VendorDto(vendor.Id, vendor.VendorId, vendor.Name, vendor.LegalName, vendor.TaxId, vendor.Form1099Category, vendor.DefaultPaymentTermId, vendor.IsActive, vendor.BackupWithholdingFlag, vendor.BackupWithholdingRate, vendor.OnHold, vendor.InsuranceCarrier, vendor.InsurancePolicyNumber, vendor.InsuranceExpiry, vendor.DiversityClassification, baDtos, vendor.CreatedOn, vendor.ModifiedOn);
    }
}