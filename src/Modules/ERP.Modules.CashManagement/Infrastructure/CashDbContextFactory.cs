// <copyright file="CashDbContextFactory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERP.Modules.CashManagement.Infrastructure;

public class CashDbContextFactory : IDesignTimeDbContextFactory<CashDbContext>
{
    public CashDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CashDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=erp;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(CashDbContextFactory).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "cash");
            });

        return new CashDbContext(optionsBuilder.Options);
    }
}
