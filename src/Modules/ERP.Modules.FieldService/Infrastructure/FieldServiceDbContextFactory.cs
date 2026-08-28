// <copyright file="FieldServiceDbContextFactory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERP.Modules.FieldService.Infrastructure;

public class FieldServiceDbContextFactory : IDesignTimeDbContextFactory<FieldServiceDbContext>
{
    public FieldServiceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FieldServiceDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=erp;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(FieldServiceDbContextFactory).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "fs");
            });

        return new FieldServiceDbContext(optionsBuilder.Options);
    }
}
