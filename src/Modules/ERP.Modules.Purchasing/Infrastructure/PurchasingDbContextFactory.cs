// <copyright file="PurchasingDbContextFactory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERP.Modules.Purchasing.Infrastructure;

public class PurchasingDbContextFactory : IDesignTimeDbContextFactory<PurchasingDbContext>
{
    public PurchasingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PurchasingDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=erp;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "pur"));

        return new PurchasingDbContext(optionsBuilder.Options);
    }
}
