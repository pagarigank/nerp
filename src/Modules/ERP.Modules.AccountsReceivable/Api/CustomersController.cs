// <copyright file="CustomersController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ar/customers")]
public class CustomersController : ControllerBase
{
    private readonly ArDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CustomersController(ArDbContext context, ICurrentUserService currentUser)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    private IQueryable<Customer> ScopedCustomers()
    {
        var query = _context.Customers.Where(c => !c.DeletedOn.HasValue);
        if (!_currentUser.IsSuperAdmin)
        {
            var ids = _currentUser.CompanyIds;
            query = query.Where(c => ids.Contains(c.CompanyId));
        }

        return query;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetListAsync(CancellationToken cancellationToken)
    {
        var customers = await ScopedCustomers()
            .OrderBy(c => c.Name)
            .Select(c => new CustomerResponse(
                c.Id,
                c.CompanyId,
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
                c.IsActive,
                c.SalesRepId,
                c.TaxCodeId,
                c.TaxExemptionCertificateId,
                c.BillingAddress,
                c.BillingCity,
                c.BillingState,
                c.BillingZipCode,
                c.BillingCountry,
                c.ShippingAddress,
                c.ShippingCity,
                c.ShippingState,
                c.ShippingZipCode,
                c.ShippingCountry))
            .ToListAsync(cancellationToken);

        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await ScopedCustomers()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer == null)
            return NotFound();

        return Ok(new CustomerResponse(
            customer.Id,
            customer.CompanyId,
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
            customer.IsActive,
            customer.SalesRepId,
            customer.TaxCodeId,
            customer.TaxExemptionCertificateId,
            customer.BillingAddress,
            customer.BillingCity,
            customer.BillingState,
            customer.BillingZipCode,
            customer.BillingCountry,
            customer.ShippingAddress,
            customer.ShippingCity,
            customer.ShippingState,
            customer.ShippingZipCode,
            customer.ShippingCountry));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = new Customer(
            request.CompanyId,
            request.CustomerId,
            request.Name,
            request.LegalName,
            request.TaxId,
            request.CreditLimit,
            request.CreditHoldDays,
            request.DefaultPaymentTermId,
            request.TaxExempt,
            request.TaxExemptCertificate,
            request.CurrencyCode,
            request.SalesRepId,
            request.TaxCodeId,
            request.TaxExemptionCertificateId,
            request.BillingAddress,
            request.BillingCity,
            request.BillingState,
            request.BillingZipCode,
            request.BillingCountry,
            request.ShippingAddress,
            request.ShippingCity,
            request.ShippingState,
            request.ShippingZipCode,
            request.ShippingCountry);

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction("GetById", new { id = customer.Id }, new CustomerResponse(
            customer.Id,
            customer.CompanyId,
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
            customer.IsActive,
            customer.SalesRepId,
            customer.TaxCodeId,
            customer.TaxExemptionCertificateId,
            customer.BillingAddress,
            customer.BillingCity,
            customer.BillingState,
            customer.BillingZipCode,
            customer.BillingCountry,
            customer.ShippingAddress,
            customer.ShippingCity,
            customer.ShippingState,
            customer.ShippingZipCode,
            customer.ShippingCountry));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await ScopedCustomers()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer == null)
            return NotFound();

        customer.Update(
            request.CompanyId,
            request.Name,
            request.LegalName,
            request.TaxId,
            request.CreditLimit,
            request.CreditHoldDays,
            request.DefaultPaymentTermId,
            request.TaxExempt,
            request.TaxExemptCertificate,
            request.CurrencyCode,
            request.SalesRepId,
            request.TaxCodeId,
            request.TaxExemptionCertificateId,
            request.BillingAddress,
            request.BillingCity,
            request.BillingState,
            request.BillingZipCode,
            request.BillingCountry,
            request.ShippingAddress,
            request.ShippingCity,
            request.ShippingState,
            request.ShippingZipCode,
            request.ShippingCountry);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new CustomerResponse(
            customer.Id,
            customer.CompanyId,
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
            customer.IsActive,
            customer.SalesRepId,
            customer.TaxCodeId,
            customer.TaxExemptionCertificateId,
            customer.BillingAddress,
            customer.BillingCity,
            customer.BillingState,
            customer.BillingZipCode,
            customer.BillingCountry,
            customer.ShippingAddress,
            customer.ShippingCity,
            customer.ShippingState,
            customer.ShippingZipCode,
            customer.ShippingCountry));
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
