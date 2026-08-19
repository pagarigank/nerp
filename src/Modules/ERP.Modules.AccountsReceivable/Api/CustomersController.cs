// <copyright file="CustomersController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ar/customers")]
public class CustomersController : ControllerBase
{
    private readonly ArDbContext _context;

    public CustomersController(ArDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetListAsync(CancellationToken cancellationToken)
    {
        var customers = await _context.Customers
            .Where(c => !c.DeletedOn.HasValue)
            .OrderBy(c => c.Name)
            .Select(c => new CustomerResponse(
                c.Id,
                c.CustomerId,
                c.Name,
                c.LegalName,
                c.TaxId,
                c.CreditLimit,
                c.CreditHoldDays,
                c.DefaultPaymentTermId,
                c.TaxExempt,
                c.TaxExemptCertificate,
                c.CurrencyCode,
                c.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && !c.DeletedOn.HasValue, cancellationToken);

        if (customer == null)
            return NotFound();

        return Ok(new CustomerResponse(
            customer.Id,
            customer.CustomerId,
            customer.Name,
            customer.LegalName,
            customer.TaxId,
            customer.CreditLimit,
            customer.CreditHoldDays,
            customer.DefaultPaymentTermId,
            customer.TaxExempt,
            customer.TaxExemptCertificate,
            customer.CurrencyCode,
            customer.IsActive));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = new Customer(
            request.CustomerId,
            request.Name,
            request.LegalName,
            request.TaxId,
            request.CreditLimit,
            request.CreditHoldDays,
            request.DefaultPaymentTermId,
            request.TaxExempt,
            request.TaxExemptCertificate,
            request.CurrencyCode);

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction("GetById", new { id = customer.Id }, new CustomerResponse(
            customer.Id,
            customer.CustomerId,
            customer.Name,
            customer.LegalName,
            customer.TaxId,
            customer.CreditLimit,
            customer.CreditHoldDays,
            customer.DefaultPaymentTermId,
            customer.TaxExempt,
            customer.TaxExemptCertificate,
            customer.CurrencyCode,
            customer.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && !c.DeletedOn.HasValue, cancellationToken);

        if (customer == null)
            return NotFound();

        customer.Update(
            request.Name,
            request.LegalName,
            request.TaxId,
            request.CreditLimit,
            request.CreditHoldDays,
            request.DefaultPaymentTermId,
            request.TaxExempt,
            request.TaxExemptCertificate,
            request.CurrencyCode);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new CustomerResponse(
            customer.Id,
            customer.CustomerId,
            customer.Name,
            customer.LegalName,
            customer.TaxId,
            customer.CreditLimit,
            customer.CreditHoldDays,
            customer.DefaultPaymentTermId,
            customer.TaxExempt,
            customer.TaxExemptCertificate,
            customer.CurrencyCode,
            customer.IsActive));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && !c.DeletedOn.HasValue, cancellationToken);

        if (customer == null)
            return NotFound();

        customer.MarkDeleted("admin");
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
