// <copyright file="SqlServerTestContainer.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.Data.SqlClient;
using Xunit;

namespace ERP.IntegrationTests.Infrastructure;

/// <summary>
/// Test database fixture. Reworked to target the local SQL Server instance
/// (localhost,1433) directly instead of spinning up a Docker container, so the
/// integration suite can run without Docker. Uses a dedicated "erp_test"
/// database to avoid clobbering real data.
/// </summary>
public class SqlServerTestContainer : IAsyncLifetime
{
    private const string DatabaseName = "erp_test";
    private const string MasterConnectionString =
        "Server=localhost,1433;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;Connection Timeout=30;";

    public string GetConnectionString(string database = DatabaseName)
    {
        var builder = new SqlConnectionStringBuilder(MasterConnectionString)
        {
            InitialCatalog = database
        };
        return builder.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        // Ensure the dedicated test database exists.
        using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $@"
            IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{DatabaseName}')
            BEGIN
                CREATE DATABASE [{DatabaseName}];
            END";
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        // Leave the test database in place so repeated runs are fast; the
        // harness cleans tables between tests via CleanDatabaseAsync.
        await Task.CompletedTask;
    }
}
