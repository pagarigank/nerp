// <copyright file="ApDbContextFactory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public class ApDbContextFactory : IDesignTimeDbContextFactory<ApDbContext>
{
    public ApDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=erp;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApDbContextFactory).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ap");
            });

        return new ApDbContext(optionsBuilder.Options);
    }
}
