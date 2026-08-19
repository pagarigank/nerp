// <copyright file="UnitOfWork.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApDbContext _context;
    private IRepository<Vendor>? _vendors;
    private IRepository<VendorBankAccount>? _vendorBankAccounts;
    private IRepository<PaymentTerm>? _paymentTerms;
    private IRepository<VoucherBatch>? _voucherBatches;
    private IRepository<Voucher>? _vouchers;
    private IRepository<VoucherDistribution>? _voucherDistributions;
    private IRepository<Payment>? _payments;
    private IRepository<PaymentLine>? _paymentLines;
    private bool _disposed;

    public UnitOfWork(ApDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IRepository<Vendor> Vendors => _vendors ??= new Repository<Vendor>(_context);
    public IRepository<VendorBankAccount> VendorBankAccounts => _vendorBankAccounts ??= new Repository<VendorBankAccount>(_context);
    public IRepository<PaymentTerm> PaymentTerms => _paymentTerms ??= new Repository<PaymentTerm>(_context);
    public IRepository<VoucherBatch> VoucherBatches => _voucherBatches ??= new Repository<VoucherBatch>(_context);
    public IRepository<Voucher> Vouchers => _vouchers ??= new Repository<Voucher>(_context);
    public IRepository<VoucherDistribution> VoucherDistributions => _voucherDistributions ??= new Repository<VoucherDistribution>(_context);
    public IRepository<Payment> Payments => _payments ??= new Repository<Payment>(_context);
    public IRepository<PaymentLine> PaymentLines => _paymentLines ??= new Repository<PaymentLine>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}
