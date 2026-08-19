// <copyright file="IUnitOfWork.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public interface IUnitOfWork : IDisposable
{
    IRepository<Vendor> Vendors { get; }
    IRepository<VendorBankAccount> VendorBankAccounts { get; }
    IRepository<PaymentTerm> PaymentTerms { get; }
    IRepository<VoucherBatch> VoucherBatches { get; }
    IRepository<Voucher> Vouchers { get; }
    IRepository<VoucherDistribution> VoucherDistributions { get; }
    IRepository<Payment> Payments { get; }
    IRepository<PaymentLine> PaymentLines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
