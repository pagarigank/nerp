// <copyright file="UnitOfWork.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Purchasing.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly PurchasingDbContext _context;

    public UnitOfWork(PurchasingDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
