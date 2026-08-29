// <copyright file="BankAccountsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cash/bank-accounts")]
public class BankAccountsController : ControllerBase
{
    private readonly CashDbContext _context;

    public BankAccountsController(CashDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankAccountResponse>>> GetListAsync(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.BankAccounts.AsNoTracking();

        query = query.ApplyCompanyScope(HttpContext, a => a.CompanyId, companyId);

        var accounts = await query
            .OrderBy(a => a.AccountCode)
            .Select(a => Map(a))
            .ToListAsync(cancellationToken);

        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankAccountDetailResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await _context.BankAccounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.Id == id && !a.DeletedOn.HasValue, cancellationToken);

        if (account == null)
            return NotFound();

        return Ok(new BankAccountDetailResponse(
            Map(account),
            account.Contacts.Select(c => new BankContactResponse(c.Id, c.Name, c.Phone, c.Email, c.Title)).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<BankAccountResponse>> CreateAsync(
        CreateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var duplicate = await _context.BankAccounts
            .AnyAsync(b => b.CompanyId == request.CompanyId && b.AccountCode == request.AccountCode && !b.DeletedOn.HasValue, cancellationToken);
        if (duplicate)
            return Conflict(ApiResponse<BankAccountResponse>.Failure(new[] { $"A bank account with code '{request.AccountCode}' already exists." }, 409));

        var account = new BankAccount(
            request.CompanyId,
            request.AccountCode,
            request.AccountName,
            request.AccountNumber,
            request.RoutingNumber,
            request.BankName,
            request.CurrencyCode,
            (BankAccountType)request.AccountType,
            request.OpeningBalance,
            request.GlAccountId);

        account.CreatedBy = "admin";
        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction("GetById", new { id = account.Id }, Map(account));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BankAccountResponse>> UpdateAsync(
        Guid id,
        UpdateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == id && !a.DeletedOn.HasValue, cancellationToken);

        if (account == null)
            return NotFound();

        account.Update(
            request.AccountName,
            request.AccountNumber,
            request.RoutingNumber,
            request.BankName,
            request.CurrencyCode,
            (BankAccountType)request.AccountType,
            request.GlAccountId);
        account.MarkModified("admin");

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Map(account));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == id && !a.DeletedOn.HasValue, cancellationToken);

        if (account == null)
            return NotFound();

        account.MarkDeleted("admin");
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<ActionResult<BankContactResponse>> AddContactAsync(
        Guid id,
        BankContactRequest request,
        CancellationToken cancellationToken)
    {
        var account = await _context.BankAccounts
            .Include(a => a.Contacts)
            .FirstOrDefaultAsync(a => a.Id == id && !a.DeletedOn.HasValue, cancellationToken);

        if (account == null)
            return NotFound();

        account.AddContact(request.Name, request.Phone, request.Email, request.Title);
        await _context.SaveChangesAsync(cancellationToken);

        var contact = account.Contacts[^1];
        return Ok(new BankContactResponse(contact.Id, contact.Name, contact.Phone, contact.Email, contact.Title));
    }

    [HttpPut("{id:guid}/contacts/{contactId:guid}")]
    public async Task<ActionResult<BankContactResponse>> UpdateContactAsync(
        Guid id,
        Guid contactId,
        BankContactRequest request,
        CancellationToken cancellationToken)
    {
        var contact = await _context.BankContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.BankAccountId == id, cancellationToken);

        if (contact == null)
            return NotFound();

        contact.Update(request.Name, request.Phone, request.Email, request.Title);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new BankContactResponse(contact.Id, contact.Name, contact.Phone, contact.Email, contact.Title));
    }

    [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
    public async Task<ActionResult> DeleteContactAsync(
        Guid id,
        Guid contactId,
        CancellationToken cancellationToken)
    {
        var contact = await _context.BankContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.BankAccountId == id, cancellationToken);

        if (contact == null)
            return NotFound();

        _context.BankContacts.Remove(contact);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<BankAccountResponse>> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == id && !a.DeletedOn.HasValue, cancellationToken);

        if (account == null)
            return NotFound();

        account.Activate();
        account.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Map(account));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<BankAccountResponse>> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == id && !a.DeletedOn.HasValue, cancellationToken);

        if (account == null)
            return NotFound();

        account.Deactivate();
        account.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Map(account));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<BankAccountResponse>> CloseAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == id && !a.DeletedOn.HasValue, cancellationToken);

        if (account == null)
            return NotFound();

        account.Close();
        account.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Map(account));
    }

    private static BankAccountResponse Map(BankAccount account) => new(
        account.Id,
        account.CompanyId,
        account.AccountCode,
        account.AccountName,
        account.AccountNumber,
        account.RoutingNumber,
        account.BankName,
        account.CurrencyCode,
        account.AccountType.ToString(),
        account.OpeningBalance,
        account.CurrentBalance,
        account.GlAccountId,
        account.Status.ToString());
}
